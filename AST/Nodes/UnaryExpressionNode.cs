using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public  class UnaryExpressionNode : IExpressionNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public INode Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public NodeKind Kind { get; set; }
        public IExpressionNode Operand { get; set; }
        public UnaryOperation Operation { get; set; }
        public ExpressionType ExpressionType { get; set; }
        public ITypeNode? ResultType { get; set; }

        public UnaryExpressionNode(
            string name,
            string module,
            IExpressionNode operand,
            UnaryOperation operation,
            ITypeNode? resultType,
            INode parent
        )
        {
            Name = name;
            Module = module;
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
