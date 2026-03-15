using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class ContinueNode : IStatementNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public StatementType StatementType { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public ContinueNode(INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.ContinueStatement;
            StatementType = StatementType.Continue;
        }

        public void Accept(IContinueVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
