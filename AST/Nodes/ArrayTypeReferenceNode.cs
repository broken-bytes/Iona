using AST.Types;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class ArrayTypeReferenceNode : ITypeReferenceNode
    {
        public ITypeReferenceNode ElementType { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }
        public INode Root => Utils.GetRoot(this);
        public TypeReferenceKind ReferenceKind { get; set; }

        public ArrayTypeReferenceNode(ITypeReferenceNode element)
        {
            ElementType = element;
            ReferenceKind = TypeReferenceKind.Array;
        }

        public override string ToString()
        {
            return $"{ElementType}[]";
        }
    }
}
