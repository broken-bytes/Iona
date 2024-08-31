using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class AssignmentNode : IStatementNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public INode Parent { get; set; }
        public NodeType Type { get; set; }
        public StatementType StatementType { get; set; }
        public AssignmentType AssignmentType { get; set; }
        public INode Root { get => Utils.GetRoot(this); }
        public INode Target { get; set; }
        public IExpressionNode Value { get; set; }

        public AssignmentNode(
            string name,
            string module,
            AssignmentType assignmentType,
            INode target,
            IExpressionNode value,
            INode parent
        )
        {
            Name = name;
            Module = module;
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
