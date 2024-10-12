using AST.Nodes;
using AST.Types;
using AST.Visitors;
using Symbols;
using Symbols.Symbols;

namespace Typeck
{
    internal class ScopeChecker : 
        IAssignmentVisitor,
        IBlockVisitor, 
        IClassVisitor, 
        IFileVisitor, 
        IFuncVisitor, 
        IIdentifierVisitor,
        IInitVisitor,
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
            if (node.Target is IdentifierNode target)
            {
                var symbol = table.FindBy(target);

                if (symbol == null)
                {
                    node.Target = new ErrorNode(
                        $"`{target.Name}` is not defined",
                        target,
                        node
                    );
                    return;
                }

            }
            else if(node.Target is MemberAccessNode member)
            {
                LookupMemberAccessSymbol(member);
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

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(IdentifierNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            // Check if the identifier is in the current (or parent) scope
            var symbol = table.FindBy(node);
        }

        public void Visit(InitNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
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

            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(PropertyNode node)
        {
            node.Status = INode.ResolutionStatus.Resolving;

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
                case FuncNode func:
                    func.Accept(this);
                    break;
                case IdentifierNode identifier:
                    identifier.Accept(this);
                    break;
                case InitNode init:
                    init.Accept(this);
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
            }
        }

        private void LookupMemberAccessSymbol(MemberAccessNode node)
        {
            // We distinguish between `self` as the target and identifiers
            if (node.Target is IdentifierNode identifier)
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
                            node.Target = new ErrorNode(
                                $"`{node.Target}` is not defined",
                                node.Target,
                                node
                            );
                        }

                        // Make sure the member is an identifier
                        if (node.Member is IdentifierNode member)
                        {
                            // Now get the member symbol
                            var memberSymbol = symbol.FindMember(member.Name);

                            if (memberSymbol == null)
                            {
                                node.Target = new ErrorNode(
                                    $"Invalid member access. `{node.Member}` does not exist on `{node.Target}`",
                                    node.Target,
                                    node
                                );

                                return;
                            }

                            node.Status = INode.ResolutionStatus.Resolved;
                        }
                    }
                } 
                else
                {
                    var symbol = table.FindBy(identifier);

                    if (symbol == null)
                    {
                        node.Target = new ErrorNode(
                            $"`{node.Target}` is not defined",
                            node.Target,
                            node.Parent
                        );

                        return;
                    }

                    // If the symbol is a prop or variable, we need to know its type, if not skip
                    // We do this because the next pass is the type checker and this pass will be called again after the type checker

                    if (symbol is PropertySymbol prop)
                    {
                        if (prop.Type.TypeKind == TypeKind.Unknown)
                        {
                            return;
                        }
                    }
                    else if (symbol is VariableSymbol variable)
                    {
                        if (variable.Type.TypeKind == TypeKind.Unknown)
                        {
                            return;
                        }
                    }
                }
            }
        }
    }
}
