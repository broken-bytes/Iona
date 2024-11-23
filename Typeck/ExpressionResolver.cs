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
        IPropAccessVisitor,
        IPropertyVisitor,
        IScopeResolutionVisitor,
        IStructVisitor
    {
        private SymbolTable table;
        private IErrorCollector _errorCollector;
        /// The type an expression shall resolve to. Used in stuff liek assignments or parameters
        private string? _contextualTypeFqn;

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

        public void Visit(AssignmentNode node)
        {
            CheckNode(node.Target);

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
                
                return;
            }

            var leftType = table.FindTypeBy(node.Root, node.Left.ResultType.Name, null);
            var rightType = table.FindTypeBy(node.Root, node.Right.ResultType.Name, null);

            if (leftType is null || rightType is null)
            {
                return;
            }
            
            var leftOp = FindMatchingOperator(leftType, leftType.FullyQualifiedName, rightType.FullyQualifiedName, _contextualTypeFqn);
            var rightOp = FindMatchingOperator(rightType, leftType.FullyQualifiedName, rightType.FullyQualifiedName,  _contextualTypeFqn);

            if (leftOp is not null)
            {
                node.Status = INode.ResolutionStatus.Resolved;
                
                return;
            }
            
            if (rightOp is not null)
            {
                node.Status = INode.ResolutionStatus.Resolved;
                
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
            // Check if the function is in scope
            var funcWasFound = table.CheckIfFuncExists(node.Root, node);

            // Free function or member function
            if (funcWasFound)
            {
                // Check if the function is a member function or a free function
                var hierarchy = ((INode)node).Hierarchy();
                var currentType = hierarchy.OfType<ITypeNode>().FirstOrDefault();

                var typeSymbol = table.FindTypeByFQN(node.Root, currentType.FullyQualifiedName);

                // Function is inside a type -> member function
                if (typeSymbol != null)
                {
                    var self = new SelfNode(node.Parent);
                    var target = new IdentifierNode(typeSymbol.Name, node.Target);
                    var methodCall = new MethodCallNode(self, target, node.Parent);
                    methodCall.Args = node.Args;
                    methodCall.Meta = node.Meta;
                    self.Parent = methodCall;
                    target.Parent = methodCall;

                    ReplaceNode(node, methodCall);
                }
            } 
            // Init
            else
            {   
                var initWasFound = table.CheckIfInitExists(node.Root, node, null);

                if (!initWasFound)
                {
                    node.Status = INode.ResolutionStatus.Failed;

                    return;
                }
                
                // When an init was found we implicitly know a type with that name exists.
                var typeSearchResult = table.FindTypeBySimpleName(node.Root, node.Target.Value);

                if (typeSearchResult.IsSuccess)
                {
                    var initCall = new InitCallNode(typeSearchResult.Unwrapped().FullyQualifiedName, node.Parent);
                    initCall.Args = node.Args;
                    initCall.Meta = node.Meta;

                    ReplaceNode(node, initCall);
                }
            }
        }

        public void Visit(FuncNode node)
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

                if (node.Status != INode.ResolutionStatus.Resolved)
                {
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

        public void Visit(PropAccessNode node)
        {
            
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
            }

            if (node.TypeNode is null)
            {
                node.Status = INode.ResolutionStatus.Failed;

                var error = CompilerErrorFactory.MissingTypeAnnotation(node.Name, node.Meta);
                _errorCollector.Collect(error);
            }
            else
            {
                // Also update the symbol table
                var symbol = table.FindBy(node) as PropertySymbol;
                var type = table.FindTypeByFQN(node.TypeNode.FullyQualifiedName);
                symbol.Type = type;
                node.Status = INode.ResolutionStatus.Resolved;
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
                var propSymbol = symbol.Symbols.OfType<PropertySymbol>().FirstOrDefault(member => member.Name == property.Value);

                if (propSymbol is null)
                {
                    node.Status = INode.ResolutionStatus.Failed;
                    var error = CompilerErrorFactory.TypeDoesNotContainProperty(node.Scope.Value, property.Value, property.Meta);
                    _errorCollector.Collect(error);
                    
                    return null;
                }

                Kind kind = Utils.SymbolKindToASTKind(propSymbol.Type.TypeKind);

                var type = new TypeReferenceNode(propSymbol.Type.Name, node)
                {
                    FullyQualifiedName = propSymbol.Type.FullyQualifiedName,
                    Assembly = propSymbol.Type.Assembly,
                    TypeKind = kind
                };

                return type;
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
                    assignment.Target = newNode;
                }
                else
                {
                    assignment.Value = newNode;
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
        }
    }
}
