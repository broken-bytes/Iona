using AST.Nodes;
using Shared;
using static AST.Nodes.INode;

namespace AST.Types
{
    public class ParameterNode : INode
    {
        public string Name { get; set; }
        public TypeReferenceNode TypeNode { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public ResolutionStatus Status { get; set; }
        public Metadata Meta { get; set; }

        public ParameterNode(string name, TypeReferenceNode typeNode, INode? parent = null)
        {
            Name = name;
            TypeNode = typeNode;
            Parent = parent;
            Type = NodeType.Parameter;
            Status = ResolutionStatus.Unresolved;
        }
    }
}
