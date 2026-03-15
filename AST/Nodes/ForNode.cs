using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class ForNode : IStatementNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public StatementType StatementType { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        /// <summary>
        /// The loop iterator variable name (e.g. "x" in `for x in 0...3`).
        /// A value of "_" means the iterator is discarded.
        /// </summary>
        public string IteratorName { get; set; }

        /// <summary>
        /// The iterable expression — either a RangeExpressionNode (for `0...3`)
        /// or any other expression that produces an iterable.
        /// </summary>
        public IExpressionNode Iterable { get; set; }

        /// <summary>
        /// The loop body block.
        /// </summary>
        public BlockNode Body { get; set; }

        public ForNode(string iteratorName, IExpressionNode iterable, BlockNode body, INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.ForLoop;
            StatementType = StatementType.For;
            IteratorName = iteratorName;
            Iterable = iterable;
            Body = body;
        }

        public void Accept(IForVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
