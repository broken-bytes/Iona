using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class MemberAccessNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public INode Left { get; set; }
        public INode Right { get; set; }
        public ExpressionType ExpressionType => ExpressionType.MemberAccess;
        public TypeReferenceNode? ResultType => null;
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public MemberAccessNode(INode target, INode member, INode? parent)
        {
            Parent = parent;
            Type = NodeType.MemberAccess;
            Left = target;
            Right = member;
        }

        public void Accept(IMemberAccessVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
