using AST.Nodes;
using AST.Types;
using AST.Visitors;
using Symbols;

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
            if (node.Target is IdentifierNode target)
            {
                var symbol = table.FindBy(target);
                Console.WriteLine($"Found symbol: {symbol}");
            }
        }

        public void Visit(BlockNode node)
        {
            currentBlock = node;

            foreach (var child in node.Children)
            {
                HandleNode(child);
                currentBlock = node;
            }
        }

        public void Visit(ClassNode node)
        {
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(FileNode node)
        {
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
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(IdentifierNode node)
        {
            // Check if the identifier is in the current (or parent) scope
            var symbol = table.FindBy(node);
            Console.WriteLine(symbol);
        }

        public void Visit(InitNode node)
        {
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(ModuleNode node)
        {
            foreach (var child in node.Children)
            {
                HandleNode(child);
            }
        }

        public void Visit(OperatorNode node)
        {
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(PropertyNode node)
        {
            
        }

        public void Visit(ReturnNode node)
        {
            if (node.Value != null)
            {
                HandleNode(node.Value);
            }
        }

        public void Visit(StructNode node)
        {
            if (node.Body != null)
            {
                node.Body.Accept(this);
            }
        }

        public void Visit(VariableNode node)
        {
            
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
    }
}
