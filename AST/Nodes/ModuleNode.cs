using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class ModuleNode : IStatementNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public StatementType StatementType { get; set; }

        public ModuleNode(string name, INode? parent)
        {
            Name = name;
            Module = "";
            Parent = parent;
            Type = NodeType.Declaration;
            StatementType = StatementType.ModuleDeclaration;
        }

        public void Accept(IModuleVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
