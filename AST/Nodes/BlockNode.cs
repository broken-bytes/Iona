using AST.Types;
using AST.Visitors;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class BlockNode : INode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public List<INode> Children { get; set; } = new List<INode>();
        public INode Root => Utils.GetRoot(this);
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public BlockNode(INode? parent)
        {
            Parent = parent;
            Type = NodeType.CodeBlock;
        }

        public void AddChild(INode child)
        {
            Children.Add(child);
            child.Parent = this;
        }

        public void Accept(IBlockVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
