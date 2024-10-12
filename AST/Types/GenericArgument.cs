using AST.Nodes;
using static AST.Nodes.INode;

namespace AST.Types
{
    public class GenericArgument : INode
    {
        public string Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public GenericArgument(string name, INode? parent = null)
        {
            Name = name;
            Parent = parent;
            Type = NodeType.GenericType;
        }
    }
}
