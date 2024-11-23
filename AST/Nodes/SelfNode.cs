using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class SelfNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public ExpressionType ExpressionType => ExpressionType.Self;
        public TypeReferenceNode? ResultType { get; set; }
        public FileNode Root { get => Utils.GetRoot(this); }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public SelfNode(INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.Self;
        }
        
        public override string ToString()
        {
            return "this";
        }
        
        public void Accept(ISelfVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
