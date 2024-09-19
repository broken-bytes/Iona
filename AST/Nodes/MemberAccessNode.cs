using AST.Types;
using AST.Visitors;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class MemberAccessNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public INode Target { get; set; }
        public INode Member { get; set; }
        public ExpressionType ExpressionType => ExpressionType.MemberAccess;
        public INode? ResultType => null;
        public Metadata Meta { get; set; }

        public MemberAccessNode(INode target, INode member, INode? parent)
        {
            Parent = parent;
            Type = NodeType.MemberAccess;
            Target = target;
            Member = member;
        }

        public void Accept(IMemberAccessVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
