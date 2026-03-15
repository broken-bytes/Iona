using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class IfNode : IStatementNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public StatementType StatementType { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        /// <summary>
        /// The condition expression (must resolve to Bool).
        /// </summary>
        public IExpressionNode Condition { get; set; }

        /// <summary>
        /// The "then" branch body.
        /// </summary>
        public BlockNode Body { get; set; }

        /// <summary>
        /// Optional "else if" / "else" branches.
        /// An else branch has Condition = null.
        /// </summary>
        public List<ElseClause> ElseClauses { get; set; } = new();

        public IfNode(IExpressionNode condition, BlockNode body, INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.IfStatement;
            StatementType = StatementType.If;
            Condition = condition;
            Body = body;
        }

        public void Accept(IIfVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// Represents an "else if" or "else" clause.
    /// When Condition is null, this is a plain "else".
    /// </summary>
    public class ElseClause
    {
        /// <summary>
        /// The condition for "else if". Null for a plain "else".
        /// </summary>
        public IExpressionNode? Condition { get; set; }

        /// <summary>
        /// The body block.
        /// </summary>
        public BlockNode Body { get; set; }

        public ElseClause(IExpressionNode? condition, BlockNode body)
        {
            Condition = condition;
            Body = body;
        }
    }
}
