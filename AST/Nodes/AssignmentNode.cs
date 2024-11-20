using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class AssignmentNode : IStatementNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public StatementType StatementType { get; set; }
        public AssignmentType AssignmentType { get; set; }
        public FileNode Root { get => Utils.GetRoot(this); }
        public INode Target { get; set; }
        public INode Value { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public AssignmentNode(
            AssignmentType assignmentType,
            INode target,
            INode value,
            INode? parent = null
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
