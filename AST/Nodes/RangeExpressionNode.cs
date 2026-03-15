using AST.Types;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    /// <summary>
    /// Represents a range expression like `0...3` (inclusive range).
    /// </summary>
    public class RangeExpressionNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public ExpressionType ExpressionType { get; set; }
        public TypeReferenceNode? ResultType { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        /// <summary>
        /// The start of the range (e.g. `0` in `0...3`).
        /// </summary>
        public IExpressionNode Start { get; set; }

        /// <summary>
        /// The end of the range (e.g. `3` in `0...3`), inclusive.
        /// </summary>
        public IExpressionNode End { get; set; }

        public RangeExpressionNode(IExpressionNode start, IExpressionNode end, INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.Expression;
            ExpressionType = ExpressionType.For; // Reuse For expression type for range
            Start = start;
            End = end;
        }
    }
}
