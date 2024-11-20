using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class UnaryExpressionNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public NodeKind Kind { get; set; }
        public INode? Operand { get; set; }
        public UnaryOperation Operation { get; set; }
        public ExpressionType ExpressionType { get; set; }
        public TypeReferenceNode? ResultType { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public UnaryExpressionNode(
            INode? operand,
            UnaryOperation operation,
            TypeReferenceNode? resultType,
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
