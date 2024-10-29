using AST.Nodes;
using Symbols.Symbols;
using Symbols;
using AST.Visitors;
using AST.Types;
using Shared;

namespace Typeck
{
    internal class TopLevelScopeResolver :
        IAssignmentVisitor,
        IBlockVisitor,
        IClassVisitor,
        IFileVisitor,
        IFuncCallVisitor,
        IFuncVisitor,
        IIdentifierVisitor,
        IInitVisitor,
        ILiteralVisitor,
        IMemberAccessVisitor,
        IModuleVisitor,
        IOperatorVisitor,
        IPropertyVisitor,
        IReturnVisitor,
        IStructVisitor,
        IVariableVisitor
    {
        private SymbolTable table;
        private IErrorCollector errorCollector;

        internal TopLevelScopeResolver(IErrorCollector errorCollector)
        {
            this.table = new SymbolTable();
            this.errorCollector = errorCollector;
        }

        internal void CheckScopes(INode ast, SymbolTable table)
        {
            this.table = table;

            if (ast is FileNode file)
            {
                file.Accept(this);
            }
        }

        public void Visit(AssignmentNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            HandleNode(node.Target);
            HandleNode(node.Value);

            if (node.Value.Status == INode.ResolutionStatus.Failed)
            {
                var error = CompilerErrorFactory.UndefinedNameError(
                    $"{node.Value}",
                    node.Value.Meta
                );
                errorCollector.Collect(error);

                return;
            }

            if (node.Target.Status == INode.ResolutionStatus.Resolved && node.Value.Status == INode.ResolutionStatus.Resolved)
            {
                node.Status = INode.ResolutionStatus.Resolved;
            }
        }

        public void Visit(BlockNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            foreach (var child in node.Children)
            {
                HandleNode(child);
            }
        }

        public void Visit(ClassNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(FileNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            foreach (var child in node.Children)
            {
                if (child is ModuleNode module)
                {
                    module.Accept(this);
                }
            }

            node.Status = INode.ResolutionStatus.Resolved;
        }

        public void Visit(FuncNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            // Check if the types of the parameters are in scope and assign the actual type symbol to them
            foreach (var param in node.Parameters)
            {
                ResolveParameter(param);
            }

            var isResolved = node.Parameters.TrueForAll(p => p.TypeNode.Status == INode.ResolutionStatus.Resolved);

            if (node.Body != null)
            {
                node.Body.Accept(this);
                isResolved &= node.Body.Status == INode.ResolutionStatus.Resolved;
            }

            if (isResolved)
            {
                node.Status = INode.ResolutionStatus.Resolved;
            }
        }

        public void Visit(FuncCallNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;
            // Check if the function is in scope
            var symbol = FindFuncSymbol(node);

            // Free function or member function
            if (symbol.IsSuccess)
            {
                // Check if the function is a member function or a free function
                var hierarchy = ((INode)node).Hierarchy();
                var currentType = hierarchy.OfType<ITypeNode>().FirstOrDefault();

                // Function is inside a type -> member function
                if (currentType != null)
                {
                    var self = new SelfNode(node.Parent);
                    var target = new IdentifierNode(symbol.Success!.Name, node.Target);
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
                var initSymbol = FindInitSymbol(node);

                if (initSymbol.IsError)
                {
                    node.Status = INode.ResolutionStatus.Failed;

                    return;
                }

                // Replace the function call with the init call
                // Get the parent type symbol of the init call
                ISymbol? typeSymbol = initSymbol.Success!;

                while (typeSymbol.Parent != null && typeSymbol.Kind != SymbolKind.Type)
                {
                    typeSymbol = typeSymbol.Parent;
                }

                if (typeSymbol.Parent == null || typeSymbol.Kind != SymbolKind.Type)
                {
                    node.Status = INode.ResolutionStatus.Failed;

                    return;
                }

                var initCall = new InitCallNode(((TypeSymbol)typeSymbol).FullyQualifiedName, node.Parent);
                initCall.Args = node.Args;
                initCall.Meta = node.Meta;

                ReplaceNode(node, initCall);
            }
        }

        public void Visit(IdentifierNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            if (node.Value == "self")
            {
                ReplaceNode(node, new SelfNode(node.Parent) { Meta = node.Meta, Status = INode.ResolutionStatus.Resolved });

                return;
            }

            // Check if the identifier is in the current (or parent) scope
            var symbol = table.FindBy(node);

            if (symbol == null)
            {
                node.Status = INode.ResolutionStatus.Failed;
            }
        }

        public void Visit(InitNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            // Check if the types of the parameters are in scope and assign the actual type symbol to them
            foreach (var param in node.Parameters)
            {
                ResolveParameter(param);
            }

            var isResolved = node.Parameters.TrueForAll(p => p.TypeNode.Status == INode.ResolutionStatus.Resolved);

            if (node.Body != null)
            {
                node.Body.Accept(this);
                isResolved &= node.Body.Status == INode.ResolutionStatus.Resolved;
            }

            if (isResolved)
            {
                node.Status = INode.ResolutionStatus.Resolved;
            }
        }

        public void Visit(LiteralNode node)
        {
            node.Status = INode.ResolutionStatus.Resolved;
        }

        public void Visit(MemberAccessNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            HandleNode(node.Left);
        }

        public void Visit(ModuleNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            foreach (var child in node.Children)
            {
                HandleNode(child);
            }
        }

        public void Visit(OperatorNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            // Check if the types of the parameters are in scope
            foreach (var param in node.Parameters)
            {
                ResolveParameter(param);
            }

            var isResolved = node.Parameters.TrueForAll(p => p.TypeNode.Status == INode.ResolutionStatus.Resolved);

            if (node.Body != null)
            {
                node.Body.Accept(this);
                isResolved &= node.Body.Status == INode.ResolutionStatus.Resolved;
            }

            if (isResolved)
            {
                node.Status = INode.ResolutionStatus.Resolved;
            }
        }

        public void Visit(PropertyNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            if (node.Value != null)
            {
                HandleNode(node.Value);
            }
        }

        public void Visit(ReturnNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            if (node.Value != null)
            {
                HandleNode(node.Value);
            }
        }

        public void Visit(StructNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(VariableNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            if (node.Value != null)
            {
                HandleNode(node.Value);
            }
        }

        private void HandleNode(INode node)
        {
            switch (node)
            {
                case AssignmentNode assignment:
                    assignment.Accept(this);
                    break;
                case BlockNode block:
                    block.Accept(this);
                    break;
                case ClassNode classNode:
                    classNode.Accept(this);
                    break;
                case FuncCallNode funcCall:
                    funcCall.Accept(this);
                    break;
                case FuncNode func:
                    func.Accept(this);
                    break;
                case IdentifierNode identifier:
                    identifier.Accept(this);
                    break;
                case InitNode init:
                    init.Accept(this);
                    break;
                case LiteralNode literal:
                    literal.Accept(this);
                    break;
                case MemberAccessNode member:
                    member.Accept(this);
                    break;
                case ModuleNode module:
                    module.Accept(this);
                    break;
                case OperatorNode op:
                    op.Accept(this);
                    break;
                case PropertyNode property:
                    property.Accept(this);
                    break;
                case ReturnNode returnNode:
                    returnNode.Accept(this);
                    break;
                case StructNode structNode:
                    structNode.Accept(this);
                    break;
                case VariableNode variableNode:
                    variableNode.Accept(this);
                    break;
            }
        }

        private void ResolveParameter(ParameterNode param)
        {
            var type = param.TypeNode;

            if (type is TypeReferenceNode typeNode)
            {
                var result = FindTypeSymbol(typeNode.Name);

                if (result.IsError)
                {
                    param.Status = INode.ResolutionStatus.Failed;

                    var error = CompilerErrorFactory.TopLevelDefinitionError(
                        typeNode.Name,
                        typeNode.Meta
                    );

                    errorCollector.Collect(error);

                    return;
                } 
                else
                {
                    typeNode.FullyQualifiedName = result.Success!.FullyQualifiedName;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private Result<TypeSymbol, SymbolError> FindTypeSymbol(string name)
        {
            List<TypeSymbol> types = new List<TypeSymbol>();

            // Check in each module for the type
            foreach (var module in table.Modules)
            {
                var found = FindTypeSymbolIn(name, module);

                if (found != null)
                {
                    types.Add(found);
                }
            }

            // If we found more than one type with the same name, we have an ambiguity error
            if (types.Count > 1)
            {
                return Result<TypeSymbol, SymbolError>.Err(new SymbolError($"Ambiguous type name `{name}`"));
            }
            else if (types.Count == 0)
            {
                return Result<TypeSymbol, SymbolError>.Err(new SymbolError($"Type `{name}` not found"));
            }

            return Result<TypeSymbol, SymbolError>.Ok(types[0]);
        }

        private TypeSymbol? FindTypeSymbolIn(string name, ISymbol symbol)
        {
            // Step 1: Check all top level types in the current module
            // This will find stuff like Foo, Foo.Bar(if Foo is a module), etc.
            // This will not find stuff like Foo.Bar.Baz, etc. as Baz is not a top level type
            var found = symbol.Symbols.OfType<TypeSymbol>().FirstOrDefault(s => s.FullyQualifiedName == name || s.Name == name);

            if (found != null)
            {
                return found;
            }

            // TODO: Step 2: Check all nested types in the current module, if name has a dot AND we found nothing in step 1
            var split = name.Split('.').ToList();

            return found;
        }

        private Result<FuncSymbol, SymbolError> FindFuncSymbol(FuncCallNode funcCallNode)
        {
            List<FuncSymbol> functions = new List<FuncSymbol>();

            // Check 1: -> Free functions in each module
            foreach (var module in table.Modules)
            {
                var found = FindFuncSymbolIn(funcCallNode.Target.Value, module);

                if (found != null)
                {
                    functions.Add(found);
                }
            }

            // If we found more than one type with the same name, we have an ambiguity error
            if (functions.Count > 1)
            {
                return Result<FuncSymbol, SymbolError>.Err(new SymbolError($"Ambiguous function name `{funcCallNode.Target.Value}`"));
            }

            // Check 2: -> Member functions in current type
            var hierachy = ((INode)funcCallNode).Hierarchy();
            hierachy.Reverse();
            var currentType = hierachy.OfType<ITypeNode>().FirstOrDefault();

            // Check if the current type is a type node
            if (currentType == null)
            {
                return Result<FuncSymbol, SymbolError>.Err(new SymbolError($"Function `{funcCallNode.Target.Value}` not found"));
            }

            // Check if the current type has a method with the name of the function call
            var typeSymbol = FindTypeSymbol(currentType.FullyQualifiedName);

            if (typeSymbol.IsError)
            {
                return Result<FuncSymbol, SymbolError>.Err(new SymbolError($"Function `{funcCallNode.Target.Value}` not found"));
            }

            var func = FindFuncSymbolIn(funcCallNode.Target.Value, typeSymbol.Success!);

            if (func != null)
            {
                return Result<FuncSymbol, SymbolError>.Ok(func);
            }

            return Result<FuncSymbol, SymbolError>.Err(new SymbolError($"Function `{funcCallNode.Target.Value}` not found"));
        }

        private FuncSymbol? FindFuncSymbolIn(string name, ISymbol symbol)
        {
            var found = symbol.Symbols.OfType<FuncSymbol>().FirstOrDefault(s => s.Name == name);

            return found;
        }

        private Result<InitSymbol, SymbolError> FindInitSymbol(FuncCallNode node)
        {
            List<InitSymbol> initSymbols = new List<InitSymbol>();

            // Check 2: -> Member functions in current type
            var hierachy = ((INode)node).Hierarchy();
            hierachy.Reverse();
            var currentType = hierachy.OfType<ITypeNode>().FirstOrDefault();

            // Check if the current type is a type node
            if (currentType == null)
            {
                return Result<InitSymbol, SymbolError>.Err(new SymbolError($"Undefined symbol {node.Target}"));
            }

            // Check if the current type has a method with the name of the function call
            var typeSymbol = FindTypeSymbol(currentType.FullyQualifiedName);

            if (typeSymbol.IsError)
            {
                return Result<InitSymbol, SymbolError>.Err(new SymbolError($"Undefined symbol {node.Target}"));
            }

            var init = FindInitSymbolIn(typeSymbol.Success!);

            if (init != null)
            {
                return Result<InitSymbol, SymbolError>.Ok(init);
            }

            return Result<InitSymbol, SymbolError>.Err(new SymbolError($"Undefined symbol {node.Target}"));
        }

        private InitSymbol? FindInitSymbolIn(ISymbol symbol)
        {
            var found = symbol.Symbols.OfType<BlockSymbol>().FirstOrDefault()?.Symbols.OfType<InitSymbol>().FirstOrDefault();

            return found;
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
                variable.Value = newNode;
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
        }
    }
}
