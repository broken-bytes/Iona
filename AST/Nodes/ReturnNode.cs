using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class ReturnNode : IStatementNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public StatementType StatementType { get; set; }
        public IExpressionNode Value { get; set; }

        public ReturnNode(IExpressionNode value, INode? parent = null)
        {
            Value = value;
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
