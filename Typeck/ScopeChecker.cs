using AST.Nodes;
using AST.Types;
using AST.Visitors;
using Symbols;
using Symbols.Symbols;
using System.Xml.Linq;

namespace Typeck
{
    internal class ScopeChecker : 
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
        private BlockNode? currentBlock;

        internal ScopeChecker()
        {
            table = new SymbolTable();
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
            currentBlock = node;

            foreach (var child in node.Children)
            {
                HandleNode(child);
                currentBlock = node;
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
        }

        public void Visit(FuncNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            // Check if the types of the parameters are in scope and assign the actual type symbol to them
            foreach (var param in node.Parameters)
            {
                ResolveParameter(node, param);
            }

            var isResolved = node.Parameters.TrueForAll(p => p.Type.Status == INode.ResolutionStatus.Resolved);

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

            var isResolved = node.Parameters.TrueForAll(p => p.Type.Status == INode.ResolutionStatus.Resolved);

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

            ResolveMemberAccessNode(node);
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

            var isResolved = node.Parameters.TrueForAll(p => p.Type.Status == INode.ResolutionStatus.Resolved);

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

        private void ResolveParameter(INode node, Parameter param)
        {
            var type = param.Type;

            if (type is TypeReferenceNode typeNode)
            {
                var module = node.Hierarchy().ToList().Find(item => item is ModuleNode);

                // First check if the type is within the module
                var moduleSymbol = table.FindBy(module);

                if (moduleSymbol == null)
                {
                    param.Type = new ErrorNode(
                        $"`{typeNode.Name}` is not defined",
                    typeNode,
                        node
                    );

                    return;
                }

                var typeSymbol = moduleSymbol.Symbols.OfType<TypeSymbol>().ToList().Find(s => s.Name == typeNode.Name);

                if (typeSymbol != null)
                {
                    var reference = new TypeReferenceNode(typeSymbol.Name, node);
                    param.Type = reference;
                    reference.Module = moduleSymbol.Name;
                    reference.Status = INode.ResolutionStatus.Resolved;
                    reference.TypeKind = Utils.SymbolKindToASTKind(typeSymbol.TypeKind);

                    // Also set the type symbol of the parameter to the resolved symbol
                    // For this we need to find the parameter symbol in the current block
                    var parentSymbol = table.FindBy(node);

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
                    param.Type = new ErrorNode(
                        $"`{typeNode.Name}` is not defined",
                    typeNode,
                        node
                    );
                }
            }
        }

        /// <summary>
        /// Takes a member access node and resolves a more concrete type for the node
        /// The type can be one of:
        /// - PropAccessNode
        /// - NestedTypeNode
        /// </summary>
        /// <param name="memberAccess"></param>
        private void ResolveMemberAccessNode(MemberAccessNode node)
        {
            ISymbol? targetSymbol = null;

            // We distinguish between `self` as the target and identifiers
            if (node.Left is IdentifierNode identifier)
            {
                if (identifier.Name == "self")
                {
                    // Find out what `self` refers to.
                    // Steps:
                    // - Self can only be found in methods and inits -> We must be in a block
                    // - Get the parent of the Block -> A function or prop
                    // - Get the parent of the func or prop -> A Block
                    // - Get the parent of the block -> The type
                    var type = (INode)identifier;

                    while (type is not ITypeNode && type.Parent != null)
                    {
                        type = type.Parent;
                    }

                    if (type is ITypeNode typeNode)
                    {
                        var symbol = table.FindBy(typeNode) as TypeSymbol;

                        if (symbol == null)
                        {
                            node.Left = new ErrorNode(
                                $"`{node.Left}` is not defined",
                                node.Left,
                                node
                            );
                        }

                        targetSymbol = symbol;
                    }
                }
                else
                {
                    var symbol = table.FindBy(identifier);

                    if (symbol == null)
                    {
                        node.Left = new ErrorNode(
                            $"`{node.Left}` is not defined",
                            node.Left,
                            node.Parent
                        );

                        return;
                    }

                    targetSymbol = symbol;
                }
            }

            if (targetSymbol == null)
            {
                node.Left = new ErrorNode(
                    $"`{node.Left}` is not defined",
                    node.Left,
                    node
                );

                return;
            }

            if (node.Right is IdentifierNode member)
            {
                ISymbol? memberSymbol = null;

                if (targetSymbol is TypeSymbol typeSymbol)
                {
                    memberSymbol = typeSymbol.FindMember(member.Name);
                }
                else if (targetSymbol is VariableSymbol variableSymbol)
                {
                    if (variableSymbol.Type.TypeKind is not TypeKind.Unknown)
                    {
                        memberSymbol = variableSymbol.Type.FindMember(member.Name);
                    }
                    else
                    {
                        return;
                    }
                }

                if (memberSymbol == null)
                {
                    node.Left = new ErrorNode(
                        $"Invalid member access. `{node.Right}` does not exist on `{node.Left}`",
                        node.Left,
                        node
                    );

                    node.Status = INode.ResolutionStatus.Failed;

                    return;
                }

                if (memberSymbol is PropertySymbol)
                {
                    // Replace the member access node with a property access node
                    var propAccess = new PropAccessNode(node.Left, member, node.Parent);

                    // Replace the member access node with the property access node
                    ReplaceMemberAccessWithPropAccess(node, propAccess);
                }
            }
        }

        private void ReplaceMemberAccessWithPropAccess(MemberAccessNode memberAccess, PropAccessNode propAccess)
        {
            if (memberAccess.Parent is AssignmentNode assignment)
            {
                if (ReferenceEquals(assignment.Target, memberAccess))
                {
                    assignment.Target = propAccess;
                }
                else if (ReferenceEquals(assignment.Value, memberAccess))
                {
                    assignment.Value = propAccess;
                }
            }
            else if (memberAccess.Parent is BinaryExpressionNode binary)
            {
                if (ReferenceEquals(binary.Left, memberAccess))
                {
                    binary.Left = propAccess;
                }
                else if (ReferenceEquals(binary.Right, memberAccess))
                {
                    binary.Right = propAccess;
                }
            }
            else if (memberAccess.Parent is BlockNode block)
            {
                // Add the prop access node before the member access node and remove the memebr access node afterwards
                var index = block.Children.IndexOf(memberAccess);
                block.Children.Insert(index, propAccess);
                block.Children.Remove(memberAccess);
            }
        }
    }
}
