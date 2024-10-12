using AST.Types;
using AST.Visitors;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class TypeReferenceNode : INode
    {
        public string FullyQualifiedName = "";
        public string Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }
        public string Module { get; set; }

        public TypeReferenceNode(string name, INode? parent = null)
        {
            Name = name;
            Parent = parent;
            Type = NodeType.TypeReference;
            Module = "";
        }

        public void Accept(ITypeReferenceVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
