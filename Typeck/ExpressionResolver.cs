using System.ComponentModel;
using System.Reflection.Metadata;
using AST;
using AST.Nodes;
using AST.Types;
using AST.Visitors;
using Shared;
using Symbols;
using Symbols.Symbols;

namespace Typeck
{
    internal class ExpressionResolver :
        IAssignmentVisitor,
        IBinaryExpressionVisitor,
        IBlockVisitor,
        IClassVisitor,
        IFileVisitor,
        IFuncCallVisitor,
        IFuncVisitor,
        IIdentifierVisitor,
        IInitCallVisitor,
        IInitVisitor,
        ILiteralVisitor,
        IModuleVisitor,
        IOperatorVisitor,
        IParameterVisitor,
        IPropAccessVisitor,
        IPropertyVisitor,
        IScopeResolutionVisitor,
        IStructVisitor,
        IVariableVisitor
    {
        private SymbolTable table;
        private IErrorCollector _errorCollector;
        /// The type an expression shall resolve to. Used in stuff like assignments or parameters
        private string? _contextualTypeFqn;
        /// The type that is the current target, eg. `foo.<>` where the type would be the type of `foo`
        private string? _currentTypeFqn;

        internal ExpressionResolver(IErrorCollector errorCollector)
        {
            table = new SymbolTable();
            _errorCollector = errorCollector;
        }

        internal void CheckScopes(INode ast, SymbolTable table)
        {
            this.table = table;
            CheckNode(ast);
        }

        public void ResolveExpressionType(INode node)
        {
            CheckNode(node);
        }

        public void Visit(AssignmentNode node)
        {
            CheckNode(node.Target);

            if (node.Target.Status == INode.ResolutionStatus.Failed)
            {
                node.Status = INode.ResolutionStatus.Failed;
                
                return;
            }

            _contextualTypeFqn = node.Target switch
            {
                PropAccessNode propAccess => propAccess.ResultType.FullyQualifiedName,
                IdentifierNode ident => ident.ResultType.FullyQualifiedName,
                ScopeResolutionNode scope => scope.ResultType.FullyQualifiedName,
                _ => _contextualTypeFqn
            };
            
            CheckNode(node.Value);

            _contextualTypeFqn = null;
        }

        public void Visit(BinaryExpressionNode node)
        {
            CheckNode(node.Left);
            CheckNode(node.Right);

            if (node.Left.ResultType is null || node.Right.ResultType is null)
            {
                return;
            }
            
            if (node.Left.ResultType?.FullyQualifiedName == node.Right.ResultType?.FullyQualifiedName)
            {
                node.Status = INode.ResolutionStatus.Resolved;
                
                node.ResultType = node.Right.ResultType;
                
                return;
            }

            var leftType = table.FindTypeByFQN(node.Root, node.Left.ResultType.FullyQualifiedName);
            var rightType = table.FindTypeByFQN(node.Root, node.Right.ResultType.FullyQualifiedName);

            if (leftType is null || rightType is null)
            {
                return;
            }
            
            var leftOp = FindMatchingOperator(leftType, leftType.FullyQualifiedName, rightType.FullyQualifiedName, _contextualTypeFqn);
            var rightOp = FindMatchingOperator(rightType, leftType.FullyQualifiedName, rightType.FullyQualifiedName,  _contextualTypeFqn);

            if (leftOp is not null)
            {
                node.Status = INode.ResolutionStatus.Resolved;
                
                node.ResultType = new TypeReferenceNode(leftOp.ReturnType.Name, node)
                {
                    FullyQualifiedName = leftOp.ReturnType.FullyQualifiedName,
                    Assembly = leftOp.ReturnType.Assembly
                };
                
                return;
            }
            
            if (rightOp is not null)
            {
                node.Status = INode.ResolutionStatus.Resolved;
                
                node.ResultType = new TypeReferenceNode(rightOp.ReturnType.Name, node)
                {
                    FullyQualifiedName = rightOp.ReturnType.FullyQualifiedName,
                    Assembly = rightOp.ReturnType.Assembly
                };
                
                return;
            }

            node.Status = INode.ResolutionStatus.Failed;
            
            var error = CompilerErrorFactory.NoBinaryOverload(
                node.Operation.CSharpOperator(),
                node.Left.ResultType.FullyQualifiedName,
                node.Right.ResultType.FullyQualifiedName,
                _contextualTypeFqn,
                node.Meta
            );
            
            _errorCollector.Collect(error);
        }

        public void Visit(BlockNode node)
        {
            foreach (var child in node.Children)
            {
                CheckNode(child);
            }
        }

        public void Visit(ClassNode node)
        {
            if (node.Body is BlockNode blockNode)
            {
                CheckNode(blockNode);
            }
        }
        
        public void Visit(FileNode node)
        {
            foreach (var child in node.Children)
            {
                CheckNode(child);
            }
        }

        public void Visit(FuncCallNode node)
        {
            // Before we do anything else, we need to resolve each arg
            foreach (var arg in node.Args)
            {
                CheckNode(arg.Value);

                if (arg.Value.Status == INode.ResolutionStatus.Failed)
                {
                    node.Status = INode.ResolutionStatus.Failed;
                    
                    return;
                }
            }
            
            var hierarchy = ((INode)node).Hierarchy();

            TypeSymbol? typeSymbol = null;
            
            // Four different cases:
            // - Direct function call `foo()`
            // - Via prop access `prop.foo()`
            // - Via self `self.foo()`
            // - Via scope `Foo::foo()`
            
            if (_currentTypeFqn != null)
            {
                typeSymbol = table.FindTypeByFQN(node.Root, _currentTypeFqn);
            }
            else
            {
                // Check if the function is a member function or a free function
                var currentType = hierarchy.OfType<ITypeNode>().FirstOrDefault();
                typeSymbol = table.FindTypeByFQN(node.Root, currentType.FullyQualifiedName);
            }
            // Check if the function is in scope
            var funcWasFound = table.CheckIfFuncExists(node.Root, typeSymbol, node);

            if (funcWasFound.IsError)
            {
                if (funcWasFound.Error!.Error is SymbolResolutionError.Ambigious)
                {
                    var error = CompilerErrorFactory.AmbigiousFunctionCall(node.Target.Value, funcWasFound.Error.Ambiguity, node.Meta);
                    
                    _errorCollector.Collect(error);
                    
                    return;
                }
            }

            if (funcWasFound.IsSuccess)
            {
                var func = funcWasFound.Unwrapped();
                node.Target.ILValue = func.CsharpName;
                
                // Return type checking
                // - We can either have generic returns, or normal returns
                if (func.Symbols.OfType<GenericParameterSymbol>().Any(symbol => symbol.Name == func.ReturnType.Name))
                {
                    var genericParam = func
                        .Symbols
                        .OfType<GenericParameterSymbol>()
                        .FirstOrDefault(symbol => symbol.Name == func.ReturnType.Name);

                    if (genericParam is null)
                    {
                        var error = CompilerErrorFactory.TopLevelDefinitionError(func.ReturnType.FullyQualifiedName, node.Meta);
                        
                        _errorCollector.Collect(error);
                        
                        node.Status = INode.ResolutionStatus.Failed;
                        
                        return;
                    }

                    var index = func.Symbols.OfType<GenericParameterSymbol>().ToList().IndexOf(genericParam);

                    if (index < node.GenericArgs.Count())
                    {
                        // We can try to infer the generic from the surrounding context
                        if (_currentTypeFqn is not null)
                        {
                            var surroundingType = table.FindTypeByFQN(node.Root, _currentTypeFqn);

                            if (surroundingType is not null)
                            {
                                node.Status = INode.ResolutionStatus.Resolved;
                                node.ResultType = new TypeReferenceNode(surroundingType.Name, node)
                                {
                                    FullyQualifiedName = surroundingType.FullyQualifiedName,
                                    Assembly = surroundingType.Assembly
                                };
                                
                                return;
                            }
                            else
                            {
                                var error = CompilerErrorFactory.CannotInferType(func.ReturnType.FullyQualifiedName, node.Meta);
                                
                                _errorCollector.Collect(error);
                                
                                node.Status = INode.ResolutionStatus.Failed;
                                
                                return;
                            }
                        }
                    }
                    
                    var genericReturnArg = node.GenericArgs[index];
                    
                    var returnTypeSymbol = table.FindTypeByFQN(node.Root, genericReturnArg.Name);

                    if (returnTypeSymbol is not null)
                    {
                        node.Status = INode.ResolutionStatus.Resolved;

                        node.ResultType = new TypeReferenceNode(returnTypeSymbol.Name, node)
                        {
                            FullyQualifiedName = returnTypeSymbol.FullyQualifiedName,
                            Assembly = returnTypeSymbol.Assembly
                        };
                    }
                    else
                    {
                        var simpleType = table.FindTypeBySimpleName(node.Root, genericReturnArg.Name);

                        if (simpleType.IsSuccess)
                        {
                            node.Status = INode.ResolutionStatus.Resolved;

                            var simpleTypeType = simpleType.Unwrapped();
                            
                            node.ResultType = new TypeReferenceNode(simpleTypeType.Name, node)
                            {
                                FullyQualifiedName = simpleTypeType.FullyQualifiedName,
                                Assembly = simpleTypeType.Assembly
                            };
                        }
                    }
                    
                }
                
                return;
            }

            var initCallNode = new InitCallNode(node.Target.Value, node.Parent);
            initCallNode.Args = node.Args;
            initCallNode.Meta = node.Meta;
            
            var initWasFound = table.CheckIfInitExists(node.Root, initCallNode);

            if (initWasFound.IsError)
            {
                node.Status = INode.ResolutionStatus.Failed;
                // The function is not part of a direct reference like self or Module:: thus we emit a top level error
                if (node.Parent is not ScopeResolutionNode and not PropAccessNode)
                {
                    var error = CompilerErrorFactory.TopLevelDefinitionError(node.Target.Value, node.Target.Meta);
                        
                    _errorCollector.Collect(error);
                }
                else
                {
                    var error = CompilerErrorFactory.TypeDoesNotContainMethod(typeSymbol!.FullyQualifiedName, node.Target.Value, node.Target.Meta);
                        
                    _errorCollector.Collect(error);
                }
                    
                return;
            }

            initCallNode.TypeFullName = initWasFound.Unwrapped().ReturnType.FullyQualifiedName;
            initCallNode.ResultType = new TypeReferenceNode(initWasFound.Unwrapped().Name, initCallNode)
            {
                FullyQualifiedName = initWasFound.Unwrapped().ReturnType.FullyQualifiedName,
                Assembly = initWasFound.Unwrapped().ReturnType.Assembly
            };
            
            ReplaceNode(node, initCallNode);
            
            initCallNode.Accept(this);
        }

        public void Visit(FuncNode node)
        {
            foreach (var @param in node.Parameters)
            {
                CheckNode(@param);
            }
            
            if (node.Body == null)
            {
                return;
            }

            foreach (var child in node.Body.Children)
            {
                CheckNode(child);
            }
        }

        public void Visit(IdentifierNode node)
        {
            if (node.Status == INode.ResolutionStatus.Resolved)
            {
                return;
            }

            // Find the type
            var symbol = table.FindBy(node);
            TypeSymbol? type = symbol switch
            {
                PropertySymbol prop => prop.Type,
                VariableSymbol var => var.Type,
                _ => null
            };

            if (type is null)
            {
                var error = CompilerErrorFactory.TopLevelDefinitionError(node.Value, node.Meta);
                _errorCollector.Collect(error);

                node.Status = INode.ResolutionStatus.Failed;
                
                return;
            }

            node.ResultType = new TypeReferenceNode(type.Name, node)
            {
                FullyQualifiedName = type.FullyQualifiedName,
                Assembly = type.Assembly,
            };
        }

        public void Visit(InitCallNode node)
        {
            // Check the expression of each arg
            foreach (var arg in node.Args)
            {
                CheckNode(arg.Value);

                if (arg.Value.Status == INode.ResolutionStatus.Failed)
                {
                    node.Status = INode.ResolutionStatus.Failed;
                    
                    return;
                }
            }
            
            // Check in the symbol table if any overload exists for the given parameters
            var type = table.FindTypeByFQN(node.Root, node.TypeFullName);

            if (type is null)
            {
                // Shall not happen as this was checked earlier
                return;
            }
            
            node.ResultType = new TypeReferenceNode(type.Name, node)
            {
                FullyQualifiedName = type.FullyQualifiedName,
                Assembly = type.Assembly,
            };
            
            // Check every init if it has matching name + expression type args
            foreach (var init in type.Symbols.OfType<InitSymbol>())
            {
                if (table.ArgsMatchParameters(init.Symbols.OfType<ParameterSymbol>().ToList(), node.Args))
                {
                    return;
                }
            }
            
            // When this is reached no overload exists
            CompilerErrorFactory.NoMatchingConstructorForArgs(
                type.FullyQualifiedName, 
                node.Args.Aggregate(new Dictionary<string, string>(), (a, b) =>
                {
                    a.Add(b.Name, b.Value.ResultType.ToString());
                    return a;
                }), 
                node.Meta
                );
        }

        public void Visit(InitNode node)
        {
            if (node.Body == null)
            {
                return;
            }

            foreach (var child in node.Body.Children)
            {
                CheckNode(child);
            }
        }

        public void Visit(LiteralNode node)
        {
            var typeNode = new TypeReferenceNode(node.LiteralType.Name(), node)
            {
                FullyQualifiedName = $"Iona.Builtins.{node.LiteralType.Name()}",
                Assembly = "Iona.Builtins",
                TypeKind = Kind.Struct
            };
            
            node.ResultType = typeNode;
        }

        public void Visit(ModuleNode node)
        {
            foreach (var child in node.Children)
            {
                CheckNode(child);
            }
        }

        public void Visit(OperatorNode node)
        {
            if (node.Body == null)
            {
                return;
            }

            foreach (var child in node.Body.Children)
            {
                CheckNode(child);
            }
        }

        public void Visit(ParameterNode node)
        {
            var type = table.FindTypeBySimpleName(node.Root, node.TypeNode.Name);

            if (type.IsSuccess)
            {
                var actualType = type.Unwrapped();
                node.TypeNode = new TypeReferenceNode(actualType.Name, node)
                {
                    FullyQualifiedName = actualType.FullyQualifiedName,
                    Assembly = actualType.Assembly
                };
                
                var symbol = table.FindBy(node);

                if (symbol is ParameterSymbol param)
                {
                    param.Type = actualType;
                }
                
                return;
            }


            if (type.Error is SymbolResolutionError.Ambigious)
            {
                var error = CompilerErrorFactory.AmbiguousTypeReference(node.TypeNode.Name, node.TypeNode.Meta);
                
                _errorCollector.Collect(error);
                
                return;
            }
        }

        public void Visit(PropAccessNode node)
        {
            // Find the object first
            TypeSymbol? objType = null;
            ISymbol? objc = null;
            
            if (_currentTypeFqn is null)
            {
                objc = table.FindBy(node.Object);

                if (objc is null)
                {
                    return;
                }
            }
            else
            {
                var typeSymbol = table.FindTypeByFQN(node.Root, _currentTypeFqn);
                objc = typeSymbol.Symbols.FirstOrDefault(symbol =>
                {
                    return symbol switch
                    {
                        PropertySymbol prop => prop.Name == node.Object.ToString(),
                        VariableSymbol var => var.Name == node.Object.ToString(),
                        ParameterSymbol param => param.Name == node.Object.ToString(),
                        _ => false
                    };
                });
            }

            if (objc is PropertySymbol prop)
            {
                objType = prop.Type;
            }
            else if (objc is VariableSymbol var)
            {
                objType = var.Type;
            }
            else if (objc is ParameterSymbol param)
            {
                objType = param.Type;
            }
            else
            {
                var error = CompilerErrorFactory.TypeDoesNotContainProperty(
                    _currentTypeFqn, 
                    node.Object.ToString(), 
                    node.Object.Meta
                );
                    
                _errorCollector.Collect(error);
                    
                Utils.FailNode(node);
                
                return;
            }
            
            // Search for the type
            var type = table.FindTypeByFQN(node.Root, objType.FullyQualifiedName);

            if (type is null)
            {
                var simpleTypeCheck = table.FindTypeBySimpleName(node.Root, objType.Name);

                if (simpleTypeCheck.IsSuccess)
                {
                    type = simpleTypeCheck.Unwrapped();
                }
                else
                {
                    var error = CompilerErrorFactory.CannotInferType(objType.Name, node.Object.Meta);
                    
                    _errorCollector.Collect(error);
                    
                    node.Status = INode.ResolutionStatus.Failed;
                    
                    return;
                }
            }
            
            _currentTypeFqn = objType.FullyQualifiedName;

            node.Object.ResultType = new TypeReferenceNode(objType.Name, node.Object)
            {
                FullyQualifiedName = objType.FullyQualifiedName,
                Assembly = objType.Assembly,
            };

            if (node.Property is IdentifierNode identifier)
            {
                var objcProp = objType.Symbols.OfType<PropertySymbol>()
                    .FirstOrDefault(prop => prop.Name == identifier.Value);

                if (objcProp is null)
                {
                    var error = CompilerErrorFactory.TypeDoesNotContainProperty(objType.FullyQualifiedName, identifier.Value, identifier.Meta);
                    
                    _errorCollector.Collect(error);
                    
                    Utils.FailNode(node);
                    
                    return;
                }
                
                // Assign the result of the prop to the root property access node result type
                PropAccessNode? root = node.Parent as PropAccessNode;

                // We are not in a chain
                if (root is null)
                {
                    _currentTypeFqn = null;
                    
                    node.Object.ResultType = new TypeReferenceNode(objcProp.Type.Name, node)
                    {
                        FullyQualifiedName = objcProp.Type.FullyQualifiedName,
                        Assembly = objcProp.Type.Assembly,
                    };

                    return;
                }

                while (root != null && root.Parent is PropAccessNode)
                {
                    root = root.Parent as PropAccessNode;
                }

                root.ResultType = new TypeReferenceNode(objcProp.Type.Name, node)
                {
                    FullyQualifiedName = objcProp.Type.FullyQualifiedName,
                    Assembly = objcProp.Type.Assembly,
                };
            }
            else
            {
                CheckNode(node.Property);
            }
            
            _currentTypeFqn = null;
        }

        public void Visit(PropertyNode node)
        {
            if (node.Status == INode.ResolutionStatus.Resolved)
            {
                return;
            }
            
            // Type inference
            if (node.Value != null)
            {
                // Resolve the value
                CheckNode(node.Value);

                node.TypeNode = node.Value.ResultType;

                // Update the symbol table
                if (node.Value.Status == INode.ResolutionStatus.Resolved)
                {
                    // We have the type
                    var symbol = table.FindBy(node);
                    var typeSymbol = table.FindTypeByFQN(node.TypeNode.FullyQualifiedName);

                    if (typeSymbol is null)
                    {
                        return;
                    }

                    if (symbol is PropertySymbol prop)
                    {
                        prop.Type = typeSymbol;
                    }
                }
                
                node.Status = node.Value.Status;
            }
            else if (node.TypeNode is null)
            {
                node.Status = INode.ResolutionStatus.Failed;

                var error = CompilerErrorFactory.MissingTypeAnnotation(node.Name, node.Meta);
                _errorCollector.Collect(error);
            }
            else
            {
                // Also update the symbol table
                var symbol = table.FindBy(node) as PropertySymbol;
                
                
                var type = table.FindTypeByFQN(node.Root, node.TypeNode.FullyQualifiedName);

                if (type is null)
                {
                    var simpleName = table.FindTypeBySimpleName(node.Root, node.TypeNode.Name);

                    if (simpleName.IsSuccess)
                    {
                        node.Status = INode.ResolutionStatus.Resolved;
                        symbol.Type = simpleName.Unwrapped();
                        
                        return;
                    }
                }
                else
                {
                    node.Status = INode.ResolutionStatus.Resolved;
                    symbol.Type = type;
                        
                    return;
                }
                
                node.Status = INode.ResolutionStatus.Failed;
                
                var error = CompilerErrorFactory.TopLevelDefinitionError(node.TypeNode.Name, node.TypeNode.Meta);
                
                _errorCollector.Collect(error);
            }
        }

        public void Visit(ScopeResolutionNode node)
        {
            var type = ResolveScopeResolutionType(node, null);

            if (type is null)
            {
                return;
            }
            
            node.ResultType = type;
            node.Status = INode.ResolutionStatus.Resolved;
        }
        
        public void Visit(StructNode node)
        {
            if (node.Body == null)
            {
                return;
            }

            foreach (var child in node.Body.Children)
            {
                CheckNode(child);
            }
        }

        public void Visit(VariableNode node)
        {
            if (node.Status == INode.ResolutionStatus.Resolved)
            {
                return;
            }
            
            // Type inference
            if (node.Value != null)
            {
                // Resolve the value
                CheckNode(node.Value);

                node.TypeNode = node.Value.ResultType;
            }

            if (node.TypeNode is null && node.Value is null)
            {
                node.Status = INode.ResolutionStatus.Failed;

                var error = CompilerErrorFactory.MissingTypeAnnotation(node.Name, node.Meta);
                _errorCollector.Collect(error);
                
                return;
            }

            // Also update the symbol table
            var symbol = table.FindBy(node) as VariableSymbol;

            if (node.TypeNode is not null)
            {
                var type = table.FindTypeByFQN(node.TypeNode.FullyQualifiedName);
                symbol.Type = type;
            }
            else
            {
                if (node.Value.Status == INode.ResolutionStatus.Failed)
                {
                    var error = CompilerErrorFactory.CannotInferType(node.Name, node.Meta);
                    
                    _errorCollector.Collect(error);
                    
                    return;
                }
                
                var resultType = table.FindTypeByFQN(node.Value.ResultType.FullyQualifiedName);
                symbol.Type = resultType;
            }

            node.Status = INode.ResolutionStatus.Resolved;
            
        }

        private void CheckNode(INode? node)
        {
            if (node == null)
            {
                return; 
            }

            switch (node)
            {
                case AssignmentNode assignmentNode:
                    assignmentNode.Accept(this);
                    break;
                case BinaryExpressionNode binaryExpressionNode:
                    binaryExpressionNode.Accept(this);
                    break;
                case BlockNode blockNode:
                    blockNode.Accept(this);
                    break;
                case ClassNode classNode:
                    classNode.Accept(this);
                    break;
                case FileNode fileNode:
                    fileNode.Accept(this);
                    break;
                case FuncCallNode funcCallNode:
                    funcCallNode.Accept(this);
                    break;
                case FuncNode funcNode:
                    funcNode.Accept(this);
                    break;
                case IdentifierNode identifier:
                    identifier.Accept(this);
                    break;
                case InitCallNode initCallNode:
                    initCallNode.Accept(this);
                    break;
                case InitNode initNode:
                    initNode.Accept(this);
                    break;
                case LiteralNode literalNode:
                    literalNode.Accept(this);
                    break;
                case ModuleNode moduleNode:
                    moduleNode.Accept(this);
                    break;
                case OperatorNode operatorNode:
                    operatorNode.Accept(this);
                    break;
                case ParameterNode parameterNode:
                    parameterNode.Accept(this);
                    break;
                case PropAccessNode propAccessNode:
                    propAccessNode.Accept(this);
                    break;
                case PropertyNode propertyNode:
                    propertyNode.Accept(this);
                    break;
                case ScopeResolutionNode scopeResolution:
                    scopeResolution.Accept(this);
                    break;
                case StructNode structNode:
                    structNode.Accept(this);
                    break;
                case VariableNode variable:
                    variable.Accept(this);
                    break;
            }
        }

        /// <summary>
        /// Tries to find an
        /// </summary>
        /// <param name="type"></param>
        /// <param name="leftFqn"></param>
        /// <param name="rightFqn"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        private OperatorSymbol? FindMatchingOperator(TypeSymbol type, string leftFqn, string rightFqn, string? reference)
        {
            var leftOp = type
                .Symbols
                .OfType<OperatorSymbol>()
                .Where(op =>
                    {
                        var parameters = op.Symbols.OfType<ParameterSymbol>().ToList();

                        if (parameters.Count != 2)
                        {
                            return false;
                        }

                        if (
                            parameters[0].Type.FullyQualifiedName == leftFqn &&
                            parameters[1].Type.FullyQualifiedName == rightFqn
                        )
                        {
                            // We need to check if we have a required return type and match against it if so

                            if (reference is not null)
                            {
                                return op.ReturnType.FullyQualifiedName == reference;
                            }

                            return true;
                        }

                        return false;
                    }
                ).FirstOrDefault();

            return leftOp;
        }
        
        private TypeReferenceNode? ResolveScopeResolutionType(ScopeResolutionNode node, ISymbol? parent)
        {
            // Find the first symbol
            ISymbol? symbol;
            if (parent == null)
            {
                symbol = table.FindTypeBy(node.Root, node.Scope.Value, null);
            }
            else
            {
                symbol = parent.Symbols.FirstOrDefault(symbolSymbol => symbolSymbol.Name == node.Scope.Value);
            }

            if (symbol is null)
            {
                node.Status = INode.ResolutionStatus.Failed;
                var error = CompilerErrorFactory.TopLevelDefinitionError(node.Scope.Value, node.Meta);
                _errorCollector.Collect(error);

                return null;
            }

            if (node.Property is IdentifierNode property)
            {
                // Could be a static prop:
                var propSymbol = symbol.Symbols.OfType<PropertySymbol>().FirstOrDefault(member => member.Name == property.Value);

                if (propSymbol is not null)
                {
                    Kind kind = Utils.SymbolKindToASTKind(propSymbol.Type.TypeKind);

                    var type = new TypeReferenceNode(propSymbol.Type.Name, node)
                    {
                        FullyQualifiedName = propSymbol.Type.FullyQualifiedName,
                        Assembly = propSymbol.Type.Assembly,
                        TypeKind = kind
                    };

                    return type;
                }

                var caseSymbol = symbol.Symbols.OfType<EnumCaseSymbol>()
                    .FirstOrDefault(@case => @case.Name == property.Value);

                if (caseSymbol is not null && symbol is TypeSymbol typeSymbol)
                {
                    // We can assume the left hand is an enum type
                    Kind kind = Utils.SymbolKindToASTKind(typeSymbol.TypeKind);

                    var type = new TypeReferenceNode(typeSymbol.Name, node)
                    {
                        FullyQualifiedName = typeSymbol.FullyQualifiedName,
                        Assembly = typeSymbol.Assembly,
                        TypeKind = kind
                    };

                    var identifier = new IdentifierNode(caseSymbol.Name);
                    identifier.ILValue = caseSymbol.CsharpName;
                    var enumAccess = new EnumCaseAccessNode(identifier, node);
                    identifier.Parent = enumAccess;
                    // Change the node to enum access
                    ReplaceNode(node.Property, enumAccess);

                    return type;
                }
                
                node.Status = INode.ResolutionStatus.Failed;
                var error = CompilerErrorFactory.TypeDoesNotContainProperty(node.Scope.Value, property.Value, property.Meta);
                _errorCollector.Collect(error);
                    
                return null;
            }
            
            if (node.Property is ScopeResolutionNode scope)
            {
                return ResolveScopeResolutionType(scope, symbol);
            }

            return null;
        }
        
        private void ReplaceNode(INode node, INode newNode)
        {
            // Case 1: Node is inside a block
            if (node.Parent is BlockNode block)
            {
                var index = block.Children.IndexOf(node);
                block.Children[index] = newNode;
            }
            // Case 2: Node is value of a variable decl
            else if (node.Parent is VariableNode variable)
            {
                variable.Value = (IExpressionNode)newNode;
            }
            // Case 3: Node is value of a property decl
            else if (node.Parent is PropertyNode property)
            {
                property.Value = (IExpressionNode)newNode;
            }
            // Case 4: Node is value of a return statement
            else if (node.Parent is ReturnNode returnNode)
            {
                returnNode.Value = (IExpressionNode)newNode;
            }
            // Case 5: Node is value of an assignment
            else if (node.Parent is AssignmentNode assignment)
            {
                if (assignment.Target == node)
                {
                    assignment.Target = (IExpressionNode)newNode;
                }
                else
                {
                    assignment.Value = (IExpressionNode)newNode;
                }
            }
            // Case 6: Node is value of a binary expression
            else if (node.Parent is BinaryExpressionNode binary)
            {
                if (binary.Left == node)
                {
                    binary.Left = (IExpressionNode)newNode;
                }
                else
                {
                    binary.Right = (IExpressionNode)newNode;
                }
            }
            // Case 7: Node is the object or prop of a property access node
            else if (node.Parent is PropAccessNode propAccess)
            {
                if (propAccess.Object == node)
                {
                    propAccess.Object = (IExpressionNode)newNode;
                }
                else
                {
                    propAccess.Property = (IExpressionNode)newNode;
                }
            }
            // Case 8: Parent is a function call and node is an arg
            else if (node.Parent is FuncCallNode funcCall)
            {
                for (int x = 0; x < funcCall.Args.Count; x++)
                {
                    if (funcCall.Args[x].Value == node)
                    {
                        funcCall.Args[x].Value = (IExpressionNode)newNode;
                    }
                }
            }
            else if (node.Parent is ScopeResolutionNode scope)
            {
                if (scope.Property == node)
                {
                    scope.Property = (IExpressionNode)newNode;
                }
            }
        }
    }
}
