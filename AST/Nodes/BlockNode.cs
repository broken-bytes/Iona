using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class BlockNode : INode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public INode Parent { get; set; }
        public NodeType Type { get; set; }
        public List<INode> Children { get; set; } = new List<INode>();
        public INode Root => Utils.GetRoot(this);

        public BlockNode(string name, string module, INode parent)
        {
            Name = name;
            Module = module;
            Parent = parent;
            Type = NodeType.CodeBlock;
        }

        public void Accept(IBlockVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
