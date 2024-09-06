using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class ModuleNode : IStatementNode
    {
        public string Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public StatementType StatementType { get; set; }
        public List<INode> Children { get; private set; } = new List<INode>();

        public ModuleNode(string name, INode? parent)
        {
            Name = name;
            Parent = parent;
            Type = NodeType.Declaration;
            StatementType = StatementType.ModuleDeclaration;
        }

        public void AddChild(INode child)
        {
            Children.Add(child);
            child.Parent = this;
        }

        public void Accept(IModuleVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
