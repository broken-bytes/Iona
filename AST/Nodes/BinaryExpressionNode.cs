using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class BinaryExpressionNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Left { get; set; }
        public INode Right { get; set; }
        public BinaryOperation Operation { get; set; }
        public ExpressionType ExpressionType { get; set; }
        public IType? ResultType { get; set; }
        public INode Root { get => Utils.GetRoot(this); }

        public BinaryExpressionNode(
            INode left,
            INode right,
            BinaryOperation operation,
            IType? resultType = null,
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
