using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class BinaryExpressionNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public IExpressionNode Left { get; set; }
        public IExpressionNode Right { get; set; }
        public BinaryOperation Operation { get; set; }
        public ExpressionType ExpressionType { get; set; }
        public Type? ResultType { get; set; }
        public INode Root { get => Utils.GetRoot(this); }

        public BinaryExpressionNode(
            IExpressionNode left,
            IExpressionNode right,
            BinaryOperation operation,
            Type? resultType = null,
            INode? parent = null
        )
        {
            Parent = parent;
            Type = NodeType.Assignment;
            Left = left;
            Right = right;
            Operation = operation;
            ResultType = resultType;
        }

        public void Accept(IBinaryExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
