using AST.Types;
using AST.Visitors;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class TypeReferenceNode : INode
    {
        public string Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public Metadata Meta { get; set; }

        public TypeReferenceNode(string name, INode? parent = null)
        {
            Name = name;
            Parent = parent;
            Type = NodeType.TypeReference;
        }

        public void Accept(ITypeReferenceVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
