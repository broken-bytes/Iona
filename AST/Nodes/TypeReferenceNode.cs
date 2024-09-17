using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class TypeReferenceNode : INode
    {
        public string Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);

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
