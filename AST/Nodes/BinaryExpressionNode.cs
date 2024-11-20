using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

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
        public TypeReferenceNode? ResultType { get; set; }
        public FileNode Root { get => Utils.GetRoot(this); }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public BinaryExpressionNode(
            IExpressionNode left,
            IExpressionNode right,
            BinaryOperation operation,
            TypeReferenceNode? resultType = null,
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
