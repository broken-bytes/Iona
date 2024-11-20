using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class ArrayNode : INode
    {
        public string Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public NodeKind Kind { get; set; }
        public INode ItemType { get; set; }
        public FileNode Root { get => Utils.GetRoot(this); }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public ArrayNode(
            string name,
            INode? parent,
            NodeType type,
            NodeKind kind,
            INode itemType
        )
        {
            Name = name;
            Parent = parent;
            Type = type;
            Kind = kind;
            ItemType = itemType;
        }

        public void Accept(IArrayVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
