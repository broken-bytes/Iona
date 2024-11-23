using AST.Nodes;
using Shared;
using static AST.Nodes.INode;

namespace AST.Types
{
    public class GenericParameter : INode
    {
        public TypeReferenceNode TypeNode { get; set; }
        public NodeType Type { get; set; }
        public INode? Parent { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public GenericParameter(string name, INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.GenericArgument;
        }
    }
}