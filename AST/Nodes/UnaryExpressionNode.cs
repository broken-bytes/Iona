using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public  class UnaryExpressionNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public NodeKind Kind { get; set; }
        public IExpressionNode Operand { get; set; }
        public UnaryOperation Operation { get; set; }
        public ExpressionType ExpressionType { get; set; }
        public Types.Type? ResultType { get; set; }

        public UnaryExpressionNode(
            IExpressionNode operand,
            UnaryOperation operation,
            Types.Type? resultType,
            INode? parent
        )
        {
            Operand = operand;
            Operation = operation;
            Parent = parent;
            Type = NodeType.Expression;
            ResultType = resultType;
            ExpressionType = ExpressionType.UnaryOperation;
        }

        public void Accept(IUnaryExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
