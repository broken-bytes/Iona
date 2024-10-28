using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class ArrayLiteralNode : IExpressionNode
    {
        public List<IExpressionNode> Values { get; set; } = new List<IExpressionNode>();
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public ExpressionType ExpressionType => ExpressionType.Literal;
        public INode? ResultType { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public ArrayLiteralNode(INode? parent = null)
        {
            Type = NodeType.ArrayLiteral;
            Parent = parent;
        }

        public void Accept(IArrayLiteralVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
