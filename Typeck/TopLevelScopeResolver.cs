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

        internal TopLevelScopeResolver()
        {
            this.table = new SymbolTable();
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
                node.Value = new ErrorNode(
                    $"`{node.Value}` is not defined",
                    node.Value,
                    node
                );

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
                ResolveParameter(node, param);
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
            var symbol = table.FindBy(node);

            Console.WriteLine($"FuncCallNode: {node}");
        }

        public void Visit(IdentifierNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

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
                ResolveParameter(node, param);
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
                ResolveParameter(node, param);
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

        private void ResolveParameter(INode node, ParameterNode param)
        {
            var type = param.TypeNode;

            if (type is TypeReferenceNode typeNode)
            {
                var modules = table.Modules;

                var typeResult = FindTypeSymbol(typeNode.Name);

                if (typeResult.IsSuccess)
                {
                    var typeSymbol = typeResult.Success;
                    var reference = new TypeReferenceNode(typeSymbol!.Name, node);
                    param.TypeNode = reference;
                    reference.FullyQualifiedName = typeSymbol.FullyQualifiedName;
                    reference.Status = INode.ResolutionStatus.Resolved;
                    reference.TypeKind = Utils.SymbolKindToASTKind(typeSymbol.TypeKind);

                    // Also set the type symbol of the parameter to the resolved symbol
                    // For this we need to find the parameter symbol in the current block
                    var parentSymbol = table.FindBy(param);

                    var parameterSymbol = parentSymbol?.Symbols.OfType<ParameterSymbol>().ToList().Find(s => s.Name == param.Name);

                    if (parameterSymbol is not null)
                    {
                        parameterSymbol.Type = typeSymbol;
                    }

                    return;
                }

                var currentType = (INode)node;

                while (currentType is not ITypeNode && currentType.Parent != null)
                {
                    currentType = currentType.Parent;
                }

                var symbol = table.FindBy(currentType);

                if (symbol == null)
                {
                    param.TypeNode = new ErrorNode(
                        $"`{typeNode.Name}` is not defined",
                    typeNode,
                        node
                    );
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private Result<TypeSymbol, TypeError> FindTypeSymbol(string name)
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
                return Result<TypeSymbol, TypeError>.Err(new TypeError($"Ambiguous type name `{name}`"));
            }
            else if (types.Count == 0)
            {
                return Result<TypeSymbol, TypeError>.Err(new TypeError($"Type `{name}` not found"));
            }

            return Result<TypeSymbol, TypeError>.Ok(types[0]);
        }

        private TypeSymbol? FindTypeSymbolIn(string name, ISymbol symbol)
        {
            var found = symbol.Symbols.OfType<TypeSymbol>().FirstOrDefault(s => s.Name == name);

            return found;
        }
    }
}
