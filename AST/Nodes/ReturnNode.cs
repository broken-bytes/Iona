using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class ReturnNode : IStatementNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public StatementType StatementType { get; set; }
        public IExpressionNode? Value { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public ReturnNode(INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.Statement;
            StatementType = StatementType.ReturnStatement;
        }

        public void Accept(IReturnVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
