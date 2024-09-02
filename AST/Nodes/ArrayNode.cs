using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class ArrayNode : INode
    {
        public string Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public NodeKind Kind { get; set; }
        public Type ItemType { get; set; }
        public INode Root { get => Utils.GetRoot(this); }

        public ArrayNode(
            string name, 
            INode? parent, 
            NodeType type, 
            NodeKind kind,
            Type itemType
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
