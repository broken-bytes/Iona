using AST.Types;
using AST.Visitors;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class ErrorNode : INode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public string Message { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public ErrorNode(string message, INode replacing, INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.Error;
            Meta = replacing.Meta;
            Message = message;
        }

        public void Accept(IErrorVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
