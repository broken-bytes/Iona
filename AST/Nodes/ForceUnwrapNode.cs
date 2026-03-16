using AST.Types;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class ForceUnwrapNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public TypeReferenceNode? ResultType { get; set; }
        public ExpressionType ExpressionType => ExpressionType.ForceUnwrap;
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        /// <summary>
        /// The expression being force-unwrapped.
        /// </summary>
        public IExpressionNode Expression { get; set; }

        public ForceUnwrapNode(IExpressionNode expression, INode? parent = null)
        {
            Expression = expression;
            Parent = parent;
            Type = NodeType.ForceUnwrap;
        }
    }
}
