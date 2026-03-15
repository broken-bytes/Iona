using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class WhileNode : IStatementNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public StatementType StatementType { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        /// <summary>
        /// The loop condition expression (must resolve to Bool).
        /// </summary>
        public IExpressionNode Condition { get; set; }

        /// <summary>
        /// The loop body block.
        /// </summary>
        public BlockNode Body { get; set; }

        public WhileNode(IExpressionNode condition, BlockNode body, INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.WhileLoop;
            StatementType = StatementType.While;
            Condition = condition;
            Body = body;
        }

        public void Accept(IWhileVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
