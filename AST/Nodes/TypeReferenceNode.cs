using AST.Types;
using AST.Visitors;
using System.Reflection;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class TypeReferenceNode : INode
    {
        public string FullyQualifiedName;
        public string Name { get; set; }
        public Kind TypeKind { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public TypeReferenceNode(string name, INode? parent = null)
        {
            Name = name;
            TypeKind = Kind.Unknown;
            Parent = parent;
            Type = NodeType.TypeReference;
        }

        public void Accept(ITypeReferenceVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
