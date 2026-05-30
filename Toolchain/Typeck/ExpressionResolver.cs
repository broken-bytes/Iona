//|--- ExpressionResolver.cs -----------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

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
        IArrayAccessVisitor,
        IAssignmentVisitor,
        IAwaitExpressionVisitor,
        IBinaryExpressionVisitor,
        IBlockVisitor,
        IBreakVisitor,
        IClassVisitor,
        IContinueVisitor,
        IFileVisitor,
        IForVisitor,
        IFuncCallVisitor,
        IFuncVisitor,
        IIdentifierVisitor,
        IInitCallVisitor,
        IInvokeExpressionVisitor,
        IArrayLiteralVisitor,
        IFunctionReferenceVisitor,
        IInitVisitor,
        IInterpolatedStringVisitor,
        ILambdaVisitor,
        IMapLiteralVisitor,
        ILiteralVisitor,
        IModuleVisitor,
        IOperatorVisitor,
        IParameterVisitor,
        IPropAccessVisitor,
        IPropertyVisitor,
        IReturnVisitor,
        IScopeResolutionVisitor,
        IRecordVisitor,
        IStructVisitor,
        ISuperVisitor,
        IVariableVisitor
    {
        private SymbolTable _table;
        private IErrorCollector _errorCollector;
        private string? _contextualTypeFqn;
        private string? _currentTypeFqn;
        private readonly Stack<(string Name, TypeSymbol Type)> _loopIterators = new();

        internal ExpressionResolver(IErrorCollector errorCollector)
        {
            _table = new SymbolTable();
            _errorCollector = errorCollector;
        }

        public void ResolveExpressionType(INode node, SymbolTable table)
        {
            _table = table;
            CheckNode(node);
        }

        public void EnterLoopScope(string iteratorName, TypeSymbol type)
        {
            _loopIterators.Push((iteratorName, type));
        }

        public void ExitLoopScope()
        {
            if (_loopIterators.Count > 0)
            {
                _loopIterators.Pop();
            }
        }

        public void Visit(AwaitExpressionNode node)
        {
            CheckNode(node.Expression);

            if (node.Expression is IExpressionNode expr && expr.ResultType != null)
            {
                node.ResultType = expr.ResultType;
            }

            // Validate that await is inside an async function
            INode? parent = node.Parent;
            while (parent != null && parent is not FuncNode)
            {
                parent = parent.Parent;
            }

            if (parent is not FuncNode { IsAsync: true })
            {
                _errorCollector.Collect(CompilerErrorFactory.TopLevelDefinitionError(
                    "await can only be used inside an async function",
                    node.Meta
                ));
                node.Status = INode.ResolutionStatus.Failed;
                return;
            }

            node.Status = INode.ResolutionStatus.Resolved;
        }

        public void Visit(AssignmentNode node)
        {
            CheckNode(node.Target);

            if (node.Target.Status == INode.ResolutionStatus.Failed)
            {
                node.Status = INode.ResolutionStatus.Failed;

                return;
            }

            // Check mutability: if the target is a let variable, emit an error
            if (node.Target is IdentifierNode targetIdent)
            {
                var symbol = _table.FindBy(targetIdent);
                if (symbol is VariableSymbol varSymbol && !varSymbol.IsMutable)
                {
                    var error = CompilerErrorFactory.ImmutableVariableAssignment(targetIdent.Value, node.Meta);
                    _errorCollector.Collect(error);
                    node.Status = INode.ResolutionStatus.Failed;
                    return;
                }

                // Check if assigning to a property inside a non-mutating function
                if (symbol is PropertySymbol)
                {
                    var enclosingFunc = FindEnclosingFunc(node);
                    if (enclosingFunc != null && !enclosingFunc.IsMutable)
                    {
                        var error = CompilerErrorFactory.MutatingInNonMutatingFunc(
                            targetIdent.Value, enclosingFunc.Name, node.Meta);
                        _errorCollector.Collect(error);
                        node.Status = INode.ResolutionStatus.Failed;
                        return;
                    }
                }
            }

            // Check if assigning to self.property inside a non-mutating function
            if (node.Target is PropAccessNode selfPropAccess && selfPropAccess.Object is SelfNode)
            {
                var enclosingFunc = FindEnclosingFunc(node);
                if (enclosingFunc != null && !enclosingFunc.IsMutable)
                {
                    var propName = (selfPropAccess.Property as IdentifierNode)?.Value ?? "unknown";
                    var error = CompilerErrorFactory.MutatingInNonMutatingFunc(
                        propName, enclosingFunc.Name, node.Meta);
                    _errorCollector.Collect(error);
                    node.Status = INode.ResolutionStatus.Failed;
                    return;
                }
            }

            _contextualTypeFqn = node.Target switch
            {
                PropAccessNode propAccess => propAccess.ResultType?.FullyQualifiedName ?? _contextualTypeFqn,
                IdentifierNode ident => ident.ResultType?.FullyQualifiedName ?? _contextualTypeFqn,
                ScopeResolutionNode scope => scope.ResultType?.FullyQualifiedName ?? _contextualTypeFqn,
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

            var leftType = _table.FindTypeByFQN(node.Root, node.Left.ResultType.FullyQualifiedName);
            var rightType = _table.FindTypeByFQN(node.Root, node.Right.ResultType.FullyQualifiedName);

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

        public void Visit(ForNode node)
        {
            // Resolve expressions in the iterable
            if (node.Iterable is RangeExpressionNode range)
            {
                CheckNode(range.Start);
                CheckNode(range.End);
            }
            else
            {
                CheckNode(node.Iterable);
            }

            // Register the iterator in loop scope for body resolution
            if (node.IteratorName != "_")
            {
                TypeSymbol? iteratorType = null;

                if (node.Iterable is RangeExpressionNode rangeExpr && rangeExpr.Start.ResultType != null)
                {
                    iteratorType = _table.FindTypeByFQN(rangeExpr.Start.ResultType.FullyQualifiedName);
                }

                iteratorType ??= new TypeSymbol("Unknown", TypeKind.Unknown);
                EnterLoopScope(node.IteratorName, iteratorType);
            }

            // Resolve expressions in the body
            if (node.Body != null)
            {
                foreach (var child in node.Body.Children)
                {
                    CheckNode(child);
                }
            }

            if (node.IteratorName != "_")
            {
                ExitLoopScope();
            }
        }

        public void Visit(BreakNode node)
        {
            // Nothing to resolve
        }

        public void Visit(ContinueNode node)
        {
            // Nothing to resolve
        }

        public void Visit(ReturnNode node)
        {
            if (node.Value != null)
            {
                CheckNode(node.Value);

                // Check return type against enclosing function's declared return type
                if (node.Value is IExpressionNode exprValue && exprValue.ResultType != null)
                {
                    // Walk up to find the enclosing FuncNode
                    INode? parent = node.Parent;
                    while (parent != null && parent is not FuncNode)
                    {
                        parent = parent.Parent;
                    }

                    if (parent is FuncNode func && func.ReturnType != null)
                    {
                        var returnTypeName = func.ReturnType.Name;
                        var exprTypeName = exprValue.ResultType.Name;
                        bool exprIsOptional = exprValue.ResultType.IsOptional;

                        // If expression is optional but return type is not, that's a mismatch
                        if (exprIsOptional && !func.ReturnType.IsOptional)
                        {
                            var error = CompilerErrorFactory.ReturnTypeMismatch(
                                returnTypeName,
                                exprTypeName + "?",
                                node.Meta
                            );
                            _errorCollector.Collect(error);
                        }
                        // If type names don't match at all
                        else if (returnTypeName != exprTypeName)
                        {
                            var error = CompilerErrorFactory.ReturnTypeMismatch(
                                returnTypeName + (func.ReturnType.IsOptional ? "?" : ""),
                                exprTypeName + (exprIsOptional ? "?" : ""),
                                node.Meta
                            );
                            _errorCollector.Collect(error);
                        }
                    }
                }
            }
        }

        public void Visit(FuncCallNode node)
        {
            // Pre-scan: if any arg is a lambda with `_inferred_` params, take a guess at the
            // expected function-type slot by looking up a single unambiguous candidate (by
            // name + arity) and reading its parameter signature. The lambda then resolves
            // with that contextual hint and untyped params get the inferred types.
            PreInferLambdaArgs(node);

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

            // First, check whether `node.Target` names a local/parameter/property whose type
            // is a function type — `let f = ::double; f(x: 5)`. That short-circuits the
            // funccall-vs-initcall lookup and treats this as a delegate invocation.
            // FindBy depends on the call target having a parented hierarchy; if that fails,
            // fall back to a manual walk through the enclosing function's symbol table.
            var bindingSymbol = _table.FindBy(node.Target) ?? FindBindingByName(node, node.Target.Value);
            TypeSymbol? bindingType = bindingSymbol switch
            {
                VariableSymbol v => v.Type,
                ParameterSymbol p => p.Type,
                PropertySymbol pr => pr.Type,
                _ => null
            };
            if (bindingType != null && IsDelegateFqn(bindingType.FullyQualifiedName))
            {
                // Recover the structural FunctionTypeNode by finding the binding's declaring
                // VariableNode/ParameterNode in the AST and reading its TypeNode.
                var fnType = FindDeclaredFunctionType(node, node.Target.Value);
                node.IsDelegateInvocation = true;
                node.Status = INode.ResolutionStatus.Resolved;
                if (fnType?.ReturnType is TypeReferenceNode rt)
                {
                    node.ResultType = new TypeReferenceNode(rt.Name, node)
                    {
                        FullyQualifiedName = rt.FullyQualifiedName,
                        Assembly = rt.Assembly,
                        TypeKind = rt.TypeKind
                    };
                }
                else
                {
                    node.ResultType = new TypeReferenceNode("Void", node)
                    {
                        FullyQualifiedName = "Iona.Builtins.Void",
                        Assembly = "Iona.Builtins"
                    };
                }
                return;
            }

            TypeSymbol? typeSymbol = null;

            // Four different cases:
            // - Direct function call `foo()`
            // - Via prop access `prop.foo()`
            // - Via self `self.foo()`
            // - Via scope `Foo::foo()`
            
            if (_currentTypeFqn != null)
            {
                typeSymbol = _table.FindTypeByFQN(node.Root, _currentTypeFqn);
            }
            else
            {
                // Check if the function is a member function or a free function
                var currentType = hierarchy.OfType<ITypeNode>().FirstOrDefault();
                if (currentType != null)
                {
                    typeSymbol = _table.FindTypeByFQN(node.Root, currentType.FullyQualifiedName);
                }
            }
            // Check if the function is in scope
            var funcWasFound = _table.CheckIfFuncExists(node.Root, typeSymbol, node);

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

                // Constraint check: each call-site `<T1, T2>` arg must satisfy the matching
                // GenericParameter's declared bound. Bounds without a value (untyped `T`) skip.
                if (!ValidateGenericConstraints(func.Symbols.OfType<GenericParameterSymbol>().ToList(),
                        node.GenericArgs, node.Meta, node.Root))
                {
                    node.Status = INode.ResolutionStatus.Failed;
                    return;
                }

                foreach (var (param, arg) in func.Symbols.OfType<ParameterSymbol>().Zip(node.Args, (p, a) => (p, a)))
                {
                    if (!param.IsOptional && arg.Value.ResultType?.IsOptional == true)
                    {
                        var error = CompilerErrorFactory.OptionalArgumentNotUnwrapped(
                            param.Name, arg.Value.ResultType.FullyQualifiedName, arg.Value.Meta);
                        _errorCollector.Collect(error);
                        node.Status = INode.ResolutionStatus.Failed;
                        return;
                    }
                }

                // Access level check
                if (_currentTypeFqn != null && func.AccessLevel != AccessLevel.Public)
                {
                    var callerType = hierarchy.OfType<ITypeNode>().FirstOrDefault();
                    var isInsideType = callerType != null && callerType.FullyQualifiedName == _currentTypeFqn;

                    var isSameModule = false;
                    if (func.AccessLevel == AccessLevel.Internal)
                    {
                        var callerModule = hierarchy.OfType<ModuleNode>().FirstOrDefault();
                        var targetModule = node.Root.Children.OfType<ModuleNode>()
                            .FirstOrDefault(m => m.Children.OfType<ITypeNode>()
                                .Any(t => t.FullyQualifiedName == _currentTypeFqn));
                        isSameModule = callerModule != null && targetModule != null
                            && callerModule.Name == targetModule.Name;
                    }

                    if (!isInsideType && !isSameModule)
                    {
                        var typeKind = typeSymbol?.TypeKind switch
                        {
                            TypeKind.Struct => "struct",
                            _ => "class"
                        };
                        var error = CompilerErrorFactory.InaccessibleMember(
                            node.Target.Value,
                            typeKind,
                            _currentTypeFqn,
                            node.Meta
                        );
                        _errorCollector.Collect(error);
                        node.Status = INode.ResolutionStatus.Failed;
                        return;
                    }
                }

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

                    // Call site supplied explicit generic args — use them. (Earlier this branch
                    // fell into `_currentTypeFqn` lookup, which wrongly returned the receiver
                    // type for instance-method calls like `h.id<Int32>(x: 42)`.)
                    if (index >= node.GenericArgs.Count())
                    {
                        // No explicit arg covers this index; fall back to the surrounding
                        // context (`self.foo() -> T` inside a generic type uses the type's T).
                        if (_currentTypeFqn is not null)
                        {
                            var surroundingType = _table.FindTypeByFQN(node.Root, _currentTypeFqn);

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
                        return;
                    }

                    var genericReturnArg = node.GenericArgs[index];
                    
                    var returnTypeSymbol = _table.FindTypeByFQN(node.Root, genericReturnArg.Name);

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
                        var simpleType = _table.FindTypeBySimpleName(node.Root, genericReturnArg.Name);

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
                
                if (node.ResultType == null)
                {
                    node.Status = INode.ResolutionStatus.Resolved;
                    // If the function returns a generic param (`T`) and the call site supplied
                    // type arguments, substitute the binding so the receiving variable gets the
                    // concrete type — `identity<Int32>(x: 42)` should be Int32, not T.
                    if (func.ReturnType.TypeKind == TypeKind.Generic && node.GenericArgs.Count > 0)
                    {
                        var paramSyms = func.Symbols.OfType<GenericParameterSymbol>().ToList();
                        var idx = paramSyms.FindIndex(g => g.Name == func.ReturnType.Name);
                        if (idx >= 0 && idx < node.GenericArgs.Count)
                        {
                            var bound = node.GenericArgs[idx].Name;
                            node.ResultType = new TypeReferenceNode(bound, node)
                            {
                                FullyQualifiedName = bound.Contains('.') ? bound : $"Iona.Builtins.{bound}",
                                Assembly = "Iona.Builtins"
                            };
                            return;
                        }
                    }
                    node.ResultType = new TypeReferenceNode(func.ReturnType.Name, node)
                    {
                        FullyQualifiedName = func.ReturnType.FullyQualifiedName,
                        Assembly = func.ReturnType.Assembly,
                        IsOptional = func.ReturnType.IsOptional,
                        // Preserve the Generic kind so the surrounding PropAccess can detect a
                        // T-typed return and substitute against the receiver's type args.
                        TypeKind = Utils.SymbolKindToASTKind(func.ReturnType.TypeKind),
                        ReferenceKind = func.ReturnType.TypeKind == TypeKind.Generic
                            ? TypeReferenceKind.Generic
                            : TypeReferenceKind.Concrete
                    };
                }

                return;
            }

            var initCallNode = new InitCallNode(node.Target.Value, node.Parent);
            initCallNode.Args = node.Args;
            initCallNode.GenericArgs = node.GenericArgs;
            initCallNode.Meta = node.Meta;
            
            var initWasFound = _table.CheckIfInitExists(node.Root, initCallNode);

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

            // Constraint check for InitCall: the type-level GenericParameterSymbols live on
            // the constructed class symbol, not the InitSymbol. Walk through the class's
            // GenericArguments and validate the call-site args against their constraints.
            var classSym = initWasFound.Unwrapped().ReturnType;
            if (!ValidateGenericConstraints(classSym.Symbols.OfType<GenericParameterSymbol>().ToList(),
                    node.GenericArgs, node.Meta, node.Root))
            {
                node.Status = INode.ResolutionStatus.Failed;
                return;
            }

            initCallNode.ResultType = new TypeReferenceNode(initWasFound.Unwrapped().Name, initCallNode)
            {
                FullyQualifiedName = initWasFound.Unwrapped().ReturnType.FullyQualifiedName,
                Assembly = initWasFound.Unwrapped().ReturnType.Assembly
            };

            // Propagate call-site generic arguments onto the ResultType so codegen types the
            // receiving variable as the closed `Box<Int32>` rather than the open `Box\`1`.
            foreach (var ga in node.GenericArgs)
            {
                initCallNode.ResultType.GenericArguments.Add(new TypeReferenceNode(ga.Name, initCallNode.ResultType)
                {
                    FullyQualifiedName = ga.Name
                });
            }

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

            // Check lambda param scopes (innermost-first). A ref to an outer-scope name from
            // inside a lambda body becomes a capture on the innermost lambda.
            foreach (var scope in _lambdaScopes)
            {
                if (scope.TryGetValue(node.Value, out var lambdaParamType))
                {
                    node.ResultType = new TypeReferenceNode(lambdaParamType.Name, node)
                    {
                        FullyQualifiedName = lambdaParamType.FullyQualifiedName,
                        Assembly = lambdaParamType.Assembly,
                        TypeKind = lambdaParamType.TypeKind
                    };
                    node.Status = INode.ResolutionStatus.Resolved;
                    return;
                }
            }

            // If inside a lambda body and the name isn't a lambda local/param, record it as
            // a capture on the innermost lambda — codegen lifts it into a closure field.
            if (_lambdaLocalNames.Count > 0 && !_lambdaLocalNames.Peek().Contains(node.Value))
            {
                // Walk all lambda local sets; if not present in any, this is captured from outside.
                bool isOuterCapture = _lambdaLocalNames.All(s => !s.Contains(node.Value));
                if (isOuterCapture)
                {
                    var enclosingLambda = FindEnclosingLambda(node);
                    if (enclosingLambda != null && !enclosingLambda.Captures.Contains(node.Value))
                    {
                        enclosingLambda.Captures.Add(node.Value);
                    }
                }
            }

            // Check loop-scoped iterators first
            foreach (var iter in _loopIterators)
            {
                if (iter.Name == node.Value)
                {
                    node.ResultType = new TypeReferenceNode(iter.Type.Name, node)
                    {
                        FullyQualifiedName = iter.Type.FullyQualifiedName,
                        Assembly = iter.Type.Assembly,
                    };
                    node.Status = INode.ResolutionStatus.Resolved;
                    return;
                }
            }

            // Find the type
            var symbol = _table.FindBy(node);
            TypeSymbol? type = symbol switch
            {
                PropertySymbol prop => prop.Type,
                VariableSymbol var => var.Type,
                ParameterSymbol param => param.Type,
                _ => null
            };

            if (type is null)
            {
                var error = CompilerErrorFactory.TopLevelDefinitionError(node.Value, node.Meta);
                _errorCollector.Collect(error);

                node.Status = INode.ResolutionStatus.Failed;

                return;
            }

            var isOptional = symbol switch
            {
                PropertySymbol prop => prop.IsOptional,
                VariableSymbol var => var.IsOptional,
                ParameterSymbol param => param.IsOptional,
                _ => false
            };

            node.ResultType = new TypeReferenceNode(type.Name, node)
            {
                FullyQualifiedName = type.FullyQualifiedName,
                Assembly = type.Assembly,
                IsOptional = isOptional
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
            var type = _table.FindTypeByFQN(node.Root, node.TypeFullName);

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
            // Carry call-site generic arguments through — otherwise the receiving variable
            // gets typed as the open generic `Box\`1` instead of `Box<Int32>`.
            foreach (var ga in node.GenericArgs)
            {
                node.ResultType.GenericArguments.Add(new TypeReferenceNode(ga.Name, node.ResultType)
                {
                    FullyQualifiedName = ga.Name
                });
            }
            
            // Check every init if it has matching name + expression type args
            foreach (var init in type.Symbols.OfType<InitSymbol>())
            {
                if (_table.ArgsMatchParameters(init.Symbols.OfType<ParameterSymbol>().ToList(), node.Args))
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

            node.Status = INode.ResolutionStatus.Resolved;
        }

        public void Visit(SuperNode node)
        {
            // Type is decided by the surrounding PropAccess/InitCall.
            node.Status = INode.ResolutionStatus.Resolved;
        }

        public void Visit(FunctionReferenceNode node)
        {
            // Resolve the FuncSymbol that the reference points to, then synthesise a
            // FunctionTypeNode capturing its signature. That node feeds both arg-type
            // matching at call sites and the delegate ctor emitted in codegen.
            FuncSymbol? func = null;

            if (node.Scope == null)
            {
                // Free function lookup: walk imported modules until we find a single match.
                foreach (var module in _table.GetImportedModules(node))
                {
                    var found = module.LookupAllSymbols(node.Name).OfType<FuncSymbol>().FirstOrDefault();
                    if (found != null) { func = found; break; }
                }
            }
            else
            {
                // `::Type.method` — static/instance method on a named type.
                var typeResult = _table.FindType(node.Root, node.Scope);
                if (typeResult.IsSuccess)
                {
                    func = typeResult.Unwrapped().LookupAllSymbols(node.Name)
                        .OfType<FuncSymbol>().FirstOrDefault();
                }
            }

            if (func == null)
            {
                var error = CompilerErrorFactory.TopLevelDefinitionError(node.ToString()!, node.Meta);
                _errorCollector.Collect(error);
                Utils.FailNode(node);
                return;
            }

            var fnType = new FunctionTypeNode(node);
            foreach (var param in func.Symbols.OfType<ParameterSymbol>())
            {
                fnType.ParameterTypes.Add(new TypeReferenceNode(param.Type.Name, fnType)
                {
                    FullyQualifiedName = param.Type.FullyQualifiedName,
                    Assembly = param.Type.Assembly,
                    TypeKind = Utils.SymbolKindToASTKind(param.Type.TypeKind)
                });
            }
            fnType.ReturnType = new TypeReferenceNode(func.ReturnType.Name, fnType)
            {
                FullyQualifiedName = func.ReturnType.FullyQualifiedName,
                Assembly = func.ReturnType.Assembly,
                TypeKind = Utils.SymbolKindToASTKind(func.ReturnType.TypeKind)
            };
            fnType.FullyQualifiedName = BuildDelegateFqn(fnType);
            fnType.Status = INode.ResolutionStatus.Resolved;

            node.ResultType = fnType;
            node.Status = INode.ResolutionStatus.Resolved;
        }

        // Find the FunctionTypeNode of a binding by walking up the AST to the enclosing
        // function and scanning its body for a matching VariableNode or ParameterNode.
        // Used to recover the precise return type for a delegate invocation.
        private FunctionTypeNode? FindDeclaredFunctionType(INode start, string name)
        {
            INode? cursor = start;
            while (cursor != null)
            {
                if (cursor is FuncNode fn)
                {
                    foreach (var param in fn.Parameters)
                    {
                        if (param.Name == name && param.TypeNode is FunctionTypeNode pft)
                        {
                            return pft;
                        }
                    }
                    if (fn.Body != null)
                    {
                        foreach (var stmt in fn.Body.Children)
                        {
                            if (stmt is VariableNode vn && vn.Name == name && vn.TypeNode is FunctionTypeNode vft)
                            {
                                return vft;
                            }
                            if (stmt is VariableNode vn2 && vn2.Name == name
                                && vn2.Value is IExpressionNode ev && ev.ResultType is FunctionTypeNode evt)
                            {
                                return evt;
                            }
                        }
                    }
                    return null;
                }
                cursor = cursor.Parent;
            }
            return null;
        }

        // Walk the AST from `start` upward, querying the enclosing func/init scope's symbol
        // for a variable or parameter with the given name. Used when FindBy fails because
        // the call target's hierarchy isn't fully parented yet.
        private ISymbol? FindBindingByName(INode start, string name)
        {
            INode? cursor = start;
            while (cursor != null)
            {
                if (cursor is FuncNode fn)
                {
                    var parentType = ((INode)fn).Hierarchy().OfType<ITypeNode>().FirstOrDefault();
                    if (parentType != null)
                    {
                        var typeRes = _table.FindType(start.Root, parentType.FullyQualifiedName);
                        if (typeRes.IsSuccess)
                        {
                            var fnSym = typeRes.Unwrapped().LookupAllSymbols(fn.Name)
                                .OfType<FuncSymbol>().FirstOrDefault();
                            var hit = fnSym?.LookupAllSymbols(name).FirstOrDefault();
                            if (hit != null) { return hit; }
                        }
                    }
                    if (fn.Parent is ModuleNode mod)
                    {
                        var modSym = _table.FindModuleByFQN(start.Root, mod.Name);
                        var fnSym = modSym?.LookupAllSymbols(fn.Name).OfType<FuncSymbol>().FirstOrDefault();
                        var hit = fnSym?.LookupAllSymbols(name).FirstOrDefault();
                        if (hit != null) { return hit; }
                    }
                }
                cursor = cursor.Parent;
            }
            return null;
        }

        // Walk the call-site `<T1, T2>` args and validate each against the declared
        // GenericParameterSymbol's Constraints (intersection / AND). A param with no
        // constraints (unbounded `T`) is always satisfied. The first failing bound emits
        // C0035 and aborts.
        private bool ValidateGenericConstraints(
            List<GenericParameterSymbol> declared,
            List<GenericArgument> callSiteArgs,
            Metadata callMeta,
            FileNode rootFile)
        {
            for (int i = 0; i < declared.Count && i < callSiteArgs.Count; i++)
            {
                var constraints = declared[i].Constraints;
                if (constraints.Count == 0) { continue; }

                var suppliedName = callSiteArgs[i].Name;
                var suppliedFqn = ResolveSuppliedFqn(suppliedName, rootFile);

                foreach (var constraint in constraints)
                {
                    // Sibling-generic constraint (`where T: U`): the supplied type for T must
                    // be a subtype of the supplied type for U at this call site.
                    if (constraint.TypeKind == TypeKind.Generic)
                    {
                        var siblingIdx = declared.FindIndex(p => p.Name == constraint.Name);
                        if (siblingIdx < 0 || siblingIdx >= callSiteArgs.Count) { continue; }
                        var siblingName = callSiteArgs[siblingIdx].Name;
                        var siblingType = _table.FindTypeByFQN(rootFile, ResolveSuppliedFqn(siblingName, rootFile))
                            ?? _table.FindTypeBySimpleName(rootFile, siblingName).Success;
                        if (siblingType == null) { continue; }
                        if (!SatisfiesConstraint(suppliedFqn, suppliedName, siblingType, rootFile))
                        {
                            _errorCollector.Collect(CompilerErrorFactory.GenericConstraintNotSatisfied(
                                suppliedName, declared[i].Name, siblingName, callMeta));
                            return false;
                        }
                        continue;
                    }

                    if (!SatisfiesConstraint(suppliedFqn, suppliedName, constraint, rootFile))
                    {
                        _errorCollector.Collect(CompilerErrorFactory.GenericConstraintNotSatisfied(
                            suppliedName, declared[i].Name, constraint.Name, callMeta));
                        return false;
                    }
                }
            }
            return true;
        }

        // When `b.value` is read and `b` is `Box<Int32>`, the prop's declared type `T`
        // should resolve to `Int32` at this use site. Returns the substituted TypeReferenceNode
        // if substitution applied, or null to let the caller fall back to the open declaration.
        private TypeReferenceNode? SubstituteGenericPropType(TypeSymbol propType, PropAccessNode node)
        {
            if (propType.TypeKind != TypeKind.Generic) { return null; }
            // Find the receiver's declared TypeNode in the AST — that's where the generic
            // args supplied at the binding live.
            if (node.Object is not IdentifierNode receiverIdent) { return null; }
            var receiverTypeNode = FindDeclaringTypeNode(node, receiverIdent.Value);
            if (receiverTypeNode == null || receiverTypeNode.GenericArguments.Count == 0) { return null; }

            // Find which position `propType.Name` (e.g. "T") occupies on the owning class's
            // generic-parameter list. We use the receiver's type FQN to look up the owning
            // class node.
            var owningClass = FindClassNodeByName(node.Root, receiverTypeNode.Name);
            if (owningClass == null) { return null; }
            var paramIdx = owningClass.GenericArguments.FindIndex(g => g.Name == propType.Name);
            if (paramIdx < 0 || paramIdx >= receiverTypeNode.GenericArguments.Count) { return null; }
            var bound = receiverTypeNode.GenericArguments[paramIdx];
            if (bound is not TypeReferenceNode boundRef) { return null; }
            return new TypeReferenceNode(boundRef.Name, node)
            {
                FullyQualifiedName = boundRef.FullyQualifiedName,
                Assembly = boundRef.Assembly,
                TypeKind = boundRef.TypeKind
            };
        }

        private static TypeReferenceNode? FindDeclaringTypeNode(INode start, string name)
        {
            INode? cursor = start;
            while (cursor != null)
            {
                if (cursor is FuncNode fn)
                {
                    foreach (var p in fn.Parameters)
                    {
                        if (p.Name == name) { return p.TypeNode; }
                    }
                    if (fn.Body != null)
                    {
                        foreach (var stmt in fn.Body.Children)
                        {
                            if (stmt is VariableNode vn && vn.Name == name && vn.TypeNode != null)
                            {
                                return vn.TypeNode;
                            }
                        }
                    }
                    return null;
                }
                cursor = cursor.Parent;
            }
            return null;
        }

        private static ClassNode? FindClassNodeByName(FileNode file, string typeName)
        {
            foreach (var mod in file.Children.OfType<ModuleNode>())
            {
                foreach (var cn in mod.Children.OfType<ClassNode>())
                {
                    if (cn.Name == typeName) { return cn; }
                }
            }
            return null;
        }

        // Best-effort FQN for a bare type name supplied at a call site. Tries the symbol
        // table by simple name first (catches user-defined types like `App.Mass`), falls
        // back to the Iona.Builtins prefix for primitives.
        private string ResolveSuppliedFqn(string suppliedName, FileNode rootFile)
        {
            if (suppliedName.Contains('.')) { return suppliedName; }
            var simple = _table.FindTypeBySimpleName(rootFile, suppliedName);
            if (simple.IsSuccess) { return simple.Unwrapped().FullyQualifiedName; }
            return $"Iona.Builtins.{suppliedName}";
        }

        private bool SatisfiesConstraint(string suppliedFqn, string suppliedSimpleName, TypeSymbol constraint, FileNode rootFile)
        {
            // Exact-FQN match — the type *is* the constraint.
            if (suppliedFqn == constraint.FullyQualifiedName) { return true; }

            // Look up the supplied type; if it inherits or implements the constraint, accept.
            var supplied = _table.FindTypeByFQN(rootFile, suppliedFqn)
                ?? _table.FindTypeBySimpleName(rootFile, suppliedSimpleName).Success;
            if (supplied == null) { return false; }

            // Inheritance chain.
            var cursor = supplied.BaseType;
            while (cursor != null)
            {
                if (cursor.FullyQualifiedName == constraint.FullyQualifiedName) { return true; }
                cursor = cursor.BaseType;
            }

            // Contract implementations.
            if (supplied.Contracts.Any(c => c.FullyQualifiedName == constraint.FullyQualifiedName))
            {
                return true;
            }

            return false;
        }

        private static bool IsDelegateFqn(string? fqn) =>
            fqn != null && (fqn == "System.Action"
                || fqn.StartsWith("System.Action`", StringComparison.Ordinal)
                || fqn.StartsWith("System.Func`", StringComparison.Ordinal));

        private FunctionTypeNode? LookupFunctionTypeFor(ISymbol? binding)
        {
            // Walk the AST hierarchy of the binding's parent to find the declared type node.
            // Falls back to null if we can't recover the structural function type — codegen
            // will still resolve via the binding's symbol-table TypeSymbol FQN.
            if (binding == null) { return null; }
            return null;
        }

        // Produces `System.Action`/`System.Action`1`/.../`System.Func`1`/... matching the C#
        // wire format, so codegen and downstream tools can resolve the right Cecil generic.
        private static string BuildDelegateFqn(FunctionTypeNode fn)
        {
            var ret = fn.ReturnType?.FullyQualifiedName;
            var isVoid = ret == null || ret == "Iona.Builtins.Void" || ret == "System.Void";
            var arity = fn.ParameterTypes.Count;
            return isVoid
                ? (arity == 0 ? "System.Action" : $"System.Action`{arity}")
                : $"System.Func`{arity + 1}";
        }

        // Stack of lambda parameter scopes — each frame maps param name → declared TypeNode.
        // Used by Visit(IdentifierNode) to resolve refs to lambda params and by capture
        // detection (anything not on this stack but reachable in an outer func scope captures).
        private readonly Stack<Dictionary<string, TypeReferenceNode>> _lambdaScopes = new();
        // Stack of names introduced inside the *current* lambda's body (locals + params).
        // When the body references a name not in this set, we record it as a capture.
        private readonly Stack<HashSet<string>> _lambdaLocalNames = new();

        public void Visit(ArrayLiteralNode node)
        {
            foreach (var v in node.Values) { CheckNode(v); }

            // Infer element type from the first non-failed value (homogeneous-element MVP).
            TypeReferenceNode? elem = null;
            foreach (var v in node.Values)
            {
                if (v.ResultType != null)
                {
                    elem = v.ResultType;
                    break;
                }
            }

            var listType = new TypeReferenceNode("List", node)
            {
                FullyQualifiedName = "Iona.Builtins.List",
                Assembly = "Iona.Builtins",
                TypeKind = Kind.Class
            };
            if (elem != null)
            {
                listType.GenericArguments.Add(new TypeReferenceNode(elem.Name, listType)
                {
                    FullyQualifiedName = elem.FullyQualifiedName,
                    Assembly = elem.Assembly,
                    TypeKind = elem.TypeKind
                });
            }
            node.ResultType = listType;
            node.Status = INode.ResolutionStatus.Resolved;
        }

        public void Visit(MapLiteralNode node)
        {
            foreach (var k in node.Keys) { CheckNode(k); }
            foreach (var v in node.Values) { CheckNode(v); }

            TypeReferenceNode? keyType = node.Keys.FirstOrDefault()?.ResultType;
            TypeReferenceNode? valType = node.Values.FirstOrDefault()?.ResultType;

            var mapType = new TypeReferenceNode("Map", node)
            {
                FullyQualifiedName = "Iona.Builtins.Map",
                Assembly = "Iona.Builtins",
                TypeKind = Kind.Class
            };
            if (keyType != null)
            {
                mapType.GenericArguments.Add(new TypeReferenceNode(keyType.Name, mapType)
                {
                    FullyQualifiedName = keyType.FullyQualifiedName,
                    Assembly = keyType.Assembly,
                    TypeKind = keyType.TypeKind
                });
            }
            if (valType != null)
            {
                mapType.GenericArguments.Add(new TypeReferenceNode(valType.Name, mapType)
                {
                    FullyQualifiedName = valType.FullyQualifiedName,
                    Assembly = valType.Assembly,
                    TypeKind = valType.TypeKind
                });
            }
            node.ResultType = mapType;
            node.Status = INode.ResolutionStatus.Resolved;
        }

        public void Visit(LambdaNode node)
        {
            // Lambdas reached via paths that don't go through TypeResolver (e.g. an IIFE
            // wrapped in `InvokeExpressionNode`) may still carry parser-shape FQNs like
            // `Int32` on their param TypeNodes. Normalise them to fully-qualified form via a
            // simple-name lookup so the body's BinaryExpression has matching operand FQNs.
            foreach (var p in node.Parameters)
            {
                if (p.TypeNode != null && !p.TypeNode.FullyQualifiedName.Contains('.'))
                {
                    var hit = _table.FindTypeBySimpleName(node.Root, p.TypeNode.Name);
                    if (hit.IsSuccess)
                    {
                        var ts = hit.Unwrapped();
                        p.TypeNode.FullyQualifiedName = ts.FullyQualifiedName;
                        p.TypeNode.Assembly = ts.Assembly;
                    }
                }
            }
            // Push a new param scope so the body resolves refs to our params first.
            var paramScope = new Dictionary<string, TypeReferenceNode>();
            foreach (var p in node.Parameters)
            {
                paramScope[p.Name] = p.TypeNode;
            }
            _lambdaScopes.Push(paramScope);
            var locals = new HashSet<string>(node.Parameters.Select(p => p.Name));
            _lambdaLocalNames.Push(locals);

            try
            {
                // Resolve body.
                if (node.Body is IExpressionNode bodyExpr)
                {
                    CheckNode(bodyExpr);
                    if (node.ReturnType == null && bodyExpr.ResultType != null)
                    {
                        node.ReturnType = new TypeReferenceNode(bodyExpr.ResultType.Name, node)
                        {
                            FullyQualifiedName = bodyExpr.ResultType.FullyQualifiedName,
                            Assembly = bodyExpr.ResultType.Assembly,
                            TypeKind = bodyExpr.ResultType.TypeKind
                        };
                    }
                }
                else if (node.Body is BlockNode block)
                {
                    foreach (var stmt in block.Children)
                    {
                        if (stmt is VariableNode vn) { locals.Add(vn.Name); }
                        CheckNode(stmt);
                    }
                }
            }
            finally
            {
                _lambdaScopes.Pop();
                _lambdaLocalNames.Pop();
            }

            // Default return type to Void if still unknown (block with no trailing expression).
            node.ReturnType ??= new TypeReferenceNode("Void", node)
            {
                FullyQualifiedName = "Iona.Builtins.Void",
                Assembly = "Iona.Builtins"
            };

            // Synthesize the function type for the lambda value.
            var fnType = new FunctionTypeNode(node);
            foreach (var p in node.Parameters)
            {
                fnType.ParameterTypes.Add(new TypeReferenceNode(p.TypeNode.Name, fnType)
                {
                    FullyQualifiedName = p.TypeNode.FullyQualifiedName,
                    Assembly = p.TypeNode.Assembly,
                    TypeKind = p.TypeNode.TypeKind
                });
            }
            fnType.ReturnType = new TypeReferenceNode(node.ReturnType.Name, fnType)
            {
                FullyQualifiedName = node.ReturnType.FullyQualifiedName,
                Assembly = node.ReturnType.Assembly,
                TypeKind = node.ReturnType.TypeKind
            };
            var arity = fnType.ParameterTypes.Count;
            var isVoid = node.ReturnType.FullyQualifiedName is "Iona.Builtins.Void" or "System.Void";
            fnType.FullyQualifiedName = isVoid
                ? (arity == 0 ? "System.Action" : $"System.Action`{arity}")
                : $"System.Func`{arity + 1}";

            node.ResultType = fnType;
            node.Status = INode.ResolutionStatus.Resolved;
        }

        public void Visit(InvokeExpressionNode node)
        {
            // Resolve args first so an `_inferred_` lambda param can pick up its concrete type
            // from the matching arg before the lambda's body is checked.
            foreach (var arg in node.Args)
            {
                CheckNode(arg.Value);
            }
            if (node.Callee is LambdaNode preLambda)
            {
                for (int i = 0; i < preLambda.Parameters.Count && i < node.Args.Count; i++)
                {
                    var p = preLambda.Parameters[i];
                    if (p.TypeNode != null
                        && p.TypeNode.Name == "_inferred_"
                        && node.Args[i].Value.ResultType is TypeReferenceNode argRt)
                    {
                        p.TypeNode = new TypeReferenceNode(argRt.Name, p)
                        {
                            FullyQualifiedName = argRt.FullyQualifiedName,
                            Assembly = argRt.Assembly,
                            TypeKind = argRt.TypeKind
                        };
                    }
                }
            }
            CheckNode(node.Callee);

            // The callee resolves to a `System.Func`/`Action` (FunctionTypeNode); the invoke's
            // result is its declared return type. Fall back to Void if we couldn't recover one.
            if (node.Callee.ResultType is FunctionTypeNode fnType && fnType.ReturnType is TypeReferenceNode rt)
            {
                node.ResultType = new TypeReferenceNode(rt.Name, node)
                {
                    FullyQualifiedName = rt.FullyQualifiedName,
                    Assembly = rt.Assembly,
                    TypeKind = rt.TypeKind
                };
            }
            else
            {
                node.ResultType = new TypeReferenceNode("Void", node)
                {
                    FullyQualifiedName = "Iona.Builtins.Void",
                    Assembly = "Iona.Builtins"
                };
            }
            node.Status = INode.ResolutionStatus.Resolved;
        }

        private static LambdaNode? FindEnclosingLambda(INode start)
        {
            INode? cursor = start.Parent;
            while (cursor != null)
            {
                if (cursor is LambdaNode lam) { return lam; }
                cursor = cursor.Parent;
            }
            return null;
        }

        // Best-effort contextual type-inference for lambda args: look up the callee by name
        // + arity, and if exactly one candidate exists, copy its param types onto any lambda
        // args whose params are still placeholders.
        private void PreInferLambdaArgs(FuncCallNode node)
        {
            bool anyLambdaNeedsHints = node.Args.Any(a => a.Value is LambdaNode lam
                && lam.Parameters.Any(p => p.TypeNode?.Name == "_inferred_"));
            if (!anyLambdaNeedsHints) { return; }

            foreach (var module in _table.GetImportedModules(node))
            {
                var candidates = module.LookupAllSymbols(node.Target.Value)
                    .OfType<FuncSymbol>()
                    .Where(f => f.Symbols.OfType<ParameterSymbol>().Count() == node.Args.Count)
                    .ToList();
                if (candidates.Count != 1) { continue; }
                var fn = candidates[0];
                var paramSymbols = fn.Symbols.OfType<ParameterSymbol>().ToList();

                for (int i = 0; i < node.Args.Count; i++)
                {
                    if (node.Args[i].Value is not LambdaNode lam) { continue; }
                    var paramType = paramSymbols[i].Type;
                    var fnTypeNode = FindDeclaredFunctionTypeForFuncParam(fn, paramSymbols[i].Name, node.Root);
                    if (fnTypeNode == null) { continue; }
                    for (int j = 0; j < lam.Parameters.Count && j < fnTypeNode.ParameterTypes.Count; j++)
                    {
                        if (lam.Parameters[j].TypeNode?.Name == "_inferred_"
                            && fnTypeNode.ParameterTypes[j] is TypeReferenceNode src)
                        {
                            lam.Parameters[j].TypeNode = new TypeReferenceNode(src.Name, lam.Parameters[j])
                            {
                                FullyQualifiedName = src.FullyQualifiedName,
                                Assembly = src.Assembly,
                                TypeKind = src.TypeKind
                            };
                        }
                    }
                    if (lam.ReturnType == null && fnTypeNode.ReturnType is TypeReferenceNode srcRet)
                    {
                        lam.ReturnType = new TypeReferenceNode(srcRet.Name, lam)
                        {
                            FullyQualifiedName = srcRet.FullyQualifiedName,
                            Assembly = srcRet.Assembly,
                            TypeKind = srcRet.TypeKind
                        };
                    }
                }
                return;
            }
        }

        // Locate the declared FunctionTypeNode for a free-func's parameter by name. MVP only
        // searches the current file's modules — cross-file inference would need a back-pointer
        // from FuncSymbol to its declaring FuncNode.
        private FunctionTypeNode? FindDeclaredFunctionTypeForFuncParam(FuncSymbol funcSym, string paramName, FileNode contextFile)
        {
            foreach (var mod in contextFile.Children.OfType<ModuleNode>())
            {
                foreach (var fn in mod.Children.OfType<FuncNode>())
                {
                    if (fn.Name != funcSym.Name) { continue; }
                    var param = fn.Parameters.FirstOrDefault(p => p.Name == paramName);
                    if (param?.TypeNode is FunctionTypeNode ft) { return ft; }
                }
            }
            return null;
        }

        public void Visit(InterpolatedStringNode node)
        {
            foreach (var seg in node.Segments)
            {
                CheckNode(seg);
            }

            node.ResultType = new TypeReferenceNode("String", node)
            {
                FullyQualifiedName = "Iona.Builtins.String",
                Assembly = "Iona.Builtins",
                TypeKind = Kind.Struct
            };
            node.Status = INode.ResolutionStatus.Resolved;
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
            var type = _table.FindTypeBySimpleName(node.Root, node.TypeNode.Name);

            if (type.IsSuccess)
            {
                var actualType = type.Unwrapped();
                node.TypeNode = new TypeReferenceNode(actualType.Name, node)
                {
                    FullyQualifiedName = actualType.FullyQualifiedName,
                    Assembly = actualType.Assembly
                };
                
                var symbol = _table.FindBy(node);

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

        public void Visit(ArrayAccessNode node)
        {
            // Resolve the array expression and index expression
            CheckNode(node.Array);
            CheckNode(node.Index);

            // The result type is the element type of the array
            // For now, mark as resolved — full element type extraction requires array type tracking
            node.Status = INode.ResolutionStatus.Resolved;

            if (node.Array is IExpressionNode arrayExpr && arrayExpr.ResultType != null)
            {
                // Copy the array's element type as the result
                // Array types in .NET have element type accessible via GetElementType
                node.ResultType = arrayExpr.ResultType;
            }
        }

        public void Visit(PropAccessNode node)
        {
            // Find the object first
            TypeSymbol? objType = null;
            ISymbol? objc = null;

            if (node.Object is SelfNode)
            {
                // `self.x`: resolve members against the enclosing type.
                var enclosingType = ((INode)node).Hierarchy().OfType<ITypeNode>().FirstOrDefault();
                if (enclosingType == null)
                {
                    Utils.FailNode(node);
                    return;
                }

                var selfTypeResult = _table.FindType(node.Root, enclosingType.FullyQualifiedName);
                if (selfTypeResult.IsError)
                {
                    var error = CompilerErrorFactory.TopLevelDefinitionError(enclosingType.FullyQualifiedName, node.Meta);
                    _errorCollector.Collect(error);
                    Utils.FailNode(node);
                    return;
                }

                objType = selfTypeResult.Unwrapped();
            }
            else if (node.Object is SuperNode)
            {
                // `super.x`: resolve members against the enclosing type's BaseType.
                var enclosingType = ((INode)node).Hierarchy().OfType<ITypeNode>().FirstOrDefault();
                if (enclosingType == null)
                {
                    Utils.FailNode(node);
                    return;
                }
                var selfTypeResult = _table.FindType(node.Root, enclosingType.FullyQualifiedName);
                if (selfTypeResult.IsError || selfTypeResult.Unwrapped().BaseType == null)
                {
                    var error = CompilerErrorFactory.TopLevelDefinitionError("super", node.Meta);
                    _errorCollector.Collect(error);
                    Utils.FailNode(node);
                    return;
                }
                objType = selfTypeResult.Unwrapped().BaseType;
            }
            else if (_currentTypeFqn is null)
            {
                objc = _table.FindBy(node.Object);

                if (objc is null)
                {
                    return;
                }
            }
            else
            {
                var typeSymbol = _table.FindType(node.Root, _currentTypeFqn);

                if (typeSymbol.IsError)
                {
                    var error = CompilerErrorFactory.TopLevelDefinitionError(_currentTypeFqn, node.Meta);
                    
                    _errorCollector.Collect(error);
                    
                    node.Status = INode.ResolutionStatus.Failed;
                    
                    return;
                }
                
                var objName = node.Object.ToString();
                var candidates = typeSymbol.Unwrapped().LookupAllSymbols(objName);
                objc = candidates.FirstOrDefault(symbol =>
                    symbol is PropertySymbol or VariableSymbol or ParameterSymbol
                );
            }

            if (objType == null && objc is PropertySymbol prop)
            {
                objType = prop.Type;

                // Check: accessing members on an optional without unwrapping
                if (prop.IsOptional && !prop.IsImplicitlyUnwrapped && !node.IsOptionalChain)
                {
                    // Check if parent is a ForceUnwrapNode — that's allowed
                    if (node.Parent is not ForceUnwrapNode)
                    {
                        var error = CompilerErrorFactory.OptionalNotUnwrapped(
                            prop.Name, objType.Name, node.Object.Meta
                        );
                        _errorCollector.Collect(error);
                        Utils.FailNode(node);
                        return;
                    }
                }
            }
            else if (objType == null && objc is VariableSymbol var)
            {
                objType = var.Type;

                if (var.IsOptional && !var.IsImplicitlyUnwrapped && !node.IsOptionalChain)
                {
                    if (node.Parent is not ForceUnwrapNode)
                    {
                        var error = CompilerErrorFactory.OptionalNotUnwrapped(
                            var.Name, objType.Name, node.Object.Meta
                        );
                        _errorCollector.Collect(error);
                        Utils.FailNode(node);
                        return;
                    }
                }
            }
            else if (objType == null && objc is ParameterSymbol param)
            {
                objType = param.Type;
            }
            else if (objType == null)
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
            var type = _table.FindType(node.Root, objType.FullyQualifiedName);

            if (type.IsError)
            {
                var error = CompilerErrorFactory.CannotInferType(objType.Name, node.Object.Meta);
                    
                _errorCollector.Collect(error);
                    
                node.Status = INode.ResolutionStatus.Failed;
                    
                return;
            }
            
            _currentTypeFqn = objType.FullyQualifiedName;

            node.Object.ResultType = new TypeReferenceNode(objType.Name, node.Object)
            {
                FullyQualifiedName = objType.FullyQualifiedName,
                Assembly = objType.Assembly,
            };

            if (node.Property is IdentifierNode identifier)
            {
                var objcProp = objType.LookupAllSymbols(identifier.Value)
                    .OfType<PropertySymbol>().FirstOrDefault();

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

                    var resultType = SubstituteGenericPropType(objcProp.Type, node);
                    node.ResultType = resultType ?? new TypeReferenceNode(objcProp.Type.Name, node)
                    {
                        FullyQualifiedName = objcProp.Type.FullyQualifiedName,
                        Assembly = objcProp.Type.Assembly,
                        IsOptional = node.IsOptionalChain,
                    };
                    if (resultType != null) { node.ResultType.IsOptional = node.IsOptionalChain; }

                    return;
                }

                while (root != null && root.Parent is PropAccessNode)
                {
                    root = root.Parent as PropAccessNode;
                }

                // If any part of the chain uses optional chaining, the result is optional
                bool chainIsOptional = node.IsOptionalChain;
                if (!chainIsOptional)
                {
                    // Walk up to check if any ancestor PropAccessNode uses optional chaining
                    var walk = node.Parent as PropAccessNode;
                    while (walk != null)
                    {
                        if (walk.IsOptionalChain) { chainIsOptional = true; break; }
                        walk = walk.Parent as PropAccessNode;
                    }
                }

                var rootResult = SubstituteGenericPropType(objcProp.Type, node);
                root.ResultType = rootResult ?? new TypeReferenceNode(objcProp.Type.Name, node)
                {
                    FullyQualifiedName = objcProp.Type.FullyQualifiedName,
                    Assembly = objcProp.Type.Assembly,
                    IsOptional = chainIsOptional,
                };
                if (rootResult != null) { root.ResultType.IsOptional = chainIsOptional; }
            }
            else
            {
                CheckNode(node.Property);

                // Propagate the property's ResultType up to the PropAccess so callers see the
                // call's return type when they read PropAccess.ResultType. If the call returns
                // a generic parameter (`get() -> T`) and the receiver carries generic args,
                // substitute the same way field reads do.
                if (node.Property is IExpressionNode propExpr && propExpr.ResultType != null)
                {
                    var propRet = propExpr.ResultType;
                    if (propRet.TypeKind == Kind.Generic
                        || propRet.ReferenceKind == TypeReferenceKind.Generic)
                    {
                        var fakeSym = new TypeSymbol(propRet.Name, TypeKind.Generic);
                        var substituted = SubstituteGenericPropType(fakeSym, node);
                        node.ResultType = substituted ?? propRet;
                    }
                    else
                    {
                        node.ResultType = propRet;
                    }
                }
            }

            _currentTypeFqn = null;
        }

        public void Visit(PropertyNode node)
        {
            if (node.Status == INode.ResolutionStatus.Resolved)
            {
                return;
            }

            // Generic-parameter type (`let value: T` inside `#over<T>`): nothing to look up
            // — DeclPass already attached a placeholder symbol. Mark resolved and bail.
            if (node.TypeNode != null
                && (node.TypeNode.ReferenceKind == TypeReferenceKind.Generic
                    || node.TypeNode.TypeKind == Kind.Generic))
            {
                node.Status = INode.ResolutionStatus.Resolved;
                return;
            }

            // Type inference (only when no explicit type annotation)
            if (node.Value != null)
            {
                // Resolve the value expression
                CheckNode(node.Value);

                // Only infer type from value if no explicit type annotation was provided
                if (node.TypeNode is null)
                {
                    node.TypeNode = node.Value.ResultType;
                }

                // Update the symbol table
                if (node.TypeNode != null && node.Value.Status == INode.ResolutionStatus.Resolved)
                {
                    // We have the type
                    var symbol = _table.FindBy(node);
                    var typeSymbol = _table.FindTypeByFQN(node.TypeNode.FullyQualifiedName);

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
                var symbol = _table.FindBy(node) as PropertySymbol;
                
                
                var type = _table.FindTypeByFQN(node.Root, node.TypeNode.FullyQualifiedName);

                if (type is null)
                {
                    var simpleName = _table.FindTypeBySimpleName(node.Root, node.TypeNode.Name);

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
        
        public void Visit(RecordNode node)
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

                // Only infer from the value when there is no explicit type annotation.
                if (node.TypeNode is null)
                {
                    node.TypeNode = node.Value.ResultType;
                }
            }

            if (node.TypeNode is null && node.Value is null)
            {
                node.Status = INode.ResolutionStatus.Failed;

                var error = CompilerErrorFactory.MissingTypeAnnotation(node.Name, node.Meta);
                _errorCollector.Collect(error);
                
                return;
            }

            // Also update the symbol table
            var symbol = _table.FindBy(node) as VariableSymbol;

            if (node.TypeNode is not null)
            {
                // Function types aren't registered in the symbol table — synthesise a parent-less
                // TypeSymbol whose Name *is* the System.Func/Action FQN so the computed
                // FullyQualifiedName property reports the right thing for delegate detection.
                if (node.TypeNode is FunctionTypeNode || IsDelegateFqn(node.TypeNode.FullyQualifiedName))
                {
                    symbol.Type = new TypeSymbol(node.TypeNode.FullyQualifiedName, TypeKind.Class);
                }
                else
                {
                    var type = _table.FindTypeByFQN(node.TypeNode.FullyQualifiedName);
                    symbol.Type = type;
                }
                symbol.IsOptional = node.TypeNode.IsOptional;
            }
            else
            {
                if (node.Value.Status == INode.ResolutionStatus.Failed || node.Value.ResultType == null)
                {
                    var error = CompilerErrorFactory.CannotInferType(node.Name, node.Meta);

                    _errorCollector.Collect(error);

                    return;
                }

                if (node.Value.ResultType is FunctionTypeNode || IsDelegateFqn(node.Value.ResultType.FullyQualifiedName))
                {
                    symbol.Type = new TypeSymbol(node.Value.ResultType.FullyQualifiedName, TypeKind.Class);
                }
                else
                {
                    var resultType = _table.FindTypeByFQN(node.Value.ResultType.FullyQualifiedName);
                    symbol.Type = resultType;
                }
                symbol.IsOptional = node.Value.ResultType.IsOptional;
            }

            node.Status = INode.ResolutionStatus.Resolved;
            
        }

        private FuncNode? FindEnclosingFunc(INode node)
        {
            INode? current = node.Parent;
            while (current != null)
            {
                if (current is FuncNode func)
                {
                    return func;
                }
                current = current.Parent;
            }
            return null;
        }

        private void CheckNode(INode? node)
        {
            if (node == null)
            {
                return; 
            }

            switch (node)
            {
                case ArrayAccessNode arrayAccessNode:
                    arrayAccessNode.Accept(this);
                    break;
                case AssignmentNode assignmentNode:
                    assignmentNode.Accept(this);
                    break;
                case AwaitExpressionNode awaitNode:
                    awaitNode.Accept(this);
                    break;
                case BinaryExpressionNode binaryExpressionNode:
                    binaryExpressionNode.Accept(this);
                    break;
                case BlockNode blockNode:
                    blockNode.Accept(this);
                    break;
                case BreakNode breakNode:
                    breakNode.Accept(this);
                    break;
                case ClassNode classNode:
                    classNode.Accept(this);
                    break;
                case ContinueNode continueNode:
                    continueNode.Accept(this);
                    break;
                case FileNode fileNode:
                    fileNode.Accept(this);
                    break;
                case ForNode forNode:
                    forNode.Accept(this);
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
                case InterpolatedStringNode interp:
                    interp.Accept(this);
                    break;
                case FunctionReferenceNode fnRef:
                    fnRef.Accept(this);
                    break;
                case LambdaNode lambda:
                    lambda.Accept(this);
                    break;
                case InvokeExpressionNode invoke:
                    invoke.Accept(this);
                    break;
                case ArrayLiteralNode arrayLit:
                    arrayLit.Accept(this);
                    break;
                case MapLiteralNode mapLit:
                    mapLit.Accept(this);
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
                case ReturnNode returnNode:
                    returnNode.Accept(this);
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
                case ForceUnwrapNode forceUnwrap:
                    ResolveForceUnwrap(forceUnwrap);
                    break;
                case UnaryExpressionNode unaryExpression:
                    ResolveUnary(unaryExpression);
                    break;
            }
        }

        private void ResolveUnary(UnaryExpressionNode node)
        {
            if (node.Operand == null)
            {
                return;
            }

            CheckNode(node.Operand);

            if (node.Operand.Status == INode.ResolutionStatus.Failed)
            {
                node.Status = INode.ResolutionStatus.Failed;
                return;
            }

            if (node.Operation == UnaryOperation.Not)
            {
                node.ResultType = new TypeReferenceNode("Bool", node)
                {
                    FullyQualifiedName = "Iona.Builtins.Bool",
                    Assembly = "Iona.Builtins"
                };
            }
            else if (node.Operand is IExpressionNode operand && operand.ResultType is not null)
            {
                node.ResultType = new TypeReferenceNode(operand.ResultType.Name, node)
                {
                    FullyQualifiedName = operand.ResultType.FullyQualifiedName,
                    Assembly = operand.ResultType.Assembly,
                    IsOptional = false
                };
            }

            node.Status = INode.ResolutionStatus.Resolved;
        }

        private void ResolveForceUnwrap(ForceUnwrapNode node)
        {
            CheckNode(node.Expression);

            if (node.Expression.Status == INode.ResolutionStatus.Failed)
            {
                node.Status = INode.ResolutionStatus.Failed;
                return;
            }

            var inner = node.Expression.ResultType;

            if (inner == null || !inner.IsOptional)
            {
                var error = CompilerErrorFactory.ForceUnwrapOfNonOptional(inner?.FullyQualifiedName ?? "expression", node.Meta);
                _errorCollector.Collect(error);
                node.Status = INode.ResolutionStatus.Failed;
                return;
            }

            node.ResultType = new TypeReferenceNode(inner.Name, node)
            {
                FullyQualifiedName = inner.FullyQualifiedName,
                Assembly = inner.Assembly,
                IsOptional = false
            };
            node.Status = INode.ResolutionStatus.Resolved;
        }

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
                symbol = _table.FindTypeBy(node.Root, node.Scope.Value, null);
            }
            else
            {
                symbol = parent.LookupSymbol(node.Scope.Value);
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
                var propSymbol = symbol.LookupAllSymbols(property.Value).OfType<PropertySymbol>().FirstOrDefault();

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

                var caseSymbol = symbol.LookupAllSymbols(property.Value)
                    .OfType<EnumCaseSymbol>().FirstOrDefault();

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
            
            if (node.Property is FuncCallNode funcCall)
            {
                // Resolve argument types first so overload resolution and InferNetTypeName work
                foreach (var arg in funcCall.Args)
                {
                    CheckNode(arg.Value);
                }

                // Static method call (e.g., Console::writeLine, Environment::getCommandLineArgs)
                var ionaName = funcCall.Target.Value;
                var funcSymbol = symbol.LookupAllSymbols(ionaName)
                    .OfType<FuncSymbol>().FirstOrDefault(f =>
                        f.Symbols.OfType<ParameterSymbol>().Count() == funcCall.Args.Count);

                if (funcSymbol?.ReturnType is TypeSymbol returnType)
                {
                    Kind kind = Utils.SymbolKindToASTKind(returnType.TypeKind);
                    var type = new TypeReferenceNode(returnType.Name, node)
                    {
                        FullyQualifiedName = returnType.FullyQualifiedName,
                        Assembly = returnType.Assembly,
                        TypeKind = kind
                    };
                    return type;
                }
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
