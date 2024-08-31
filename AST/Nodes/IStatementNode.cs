using AST.Types;

namespace AST.Nodes
{
    public interface IStatementNode : INode
    {
        public StatementType StatementType { get; set; }
    }
}
