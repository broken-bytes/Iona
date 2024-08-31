using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class BinaryExpressionNode : IExpressionNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public INode Parent { get; set; }
        public NodeType Type { get; set; }
        public IExpressionNode Left { get; set; }
        public IExpressionNode Right { get; set; }
        public BinaryOperation Operation { get; set; }
        public ExpressionType ExpressionType { get; set; }
        public ITypeNode? ResultType { get; set; }

        public AssignmentType AssignmentType { get; set; }
        public INode Root { get => Utils.GetRoot(this); }

        public BinaryExpressionNode(
            string name,
            string module,
            IExpressionNode left,
            IExpressionNode right,
            BinaryOperation operation,
            ITypeNode? resultType,
            AssignmentType assignmentType,
            INode parent
        )
        {
            Name = name;
            Module = module;
            Parent = parent;
            Type = NodeType.Assignment;
            Left = left;
            Right = right;
            Operation = operation;
            ResultType = resultType;
            AssignmentType = assignmentType;
        }

        public void Accept(IBinaryExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
