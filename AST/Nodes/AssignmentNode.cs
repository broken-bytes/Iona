using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class AssignmentNode : IStatementNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public StatementType StatementType { get; set; }
        public AssignmentType AssignmentType { get; set; }
        public INode Root { get => Utils.GetRoot(this); }
        public INode Target { get; set; }
        public IExpressionNode Value { get; set; }

        public AssignmentNode(
            AssignmentType assignmentType,
            INode target,
            IExpressionNode value,
            INode? parent
        )
        {
            Parent = parent;
            StatementType = StatementType.VariableAssignment;
            Type = NodeType.Assignment;
            AssignmentType = assignmentType;
            Target = target;
            Value = value;
        }

        public void Accept(IAssignmentVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
