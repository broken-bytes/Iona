using AST.Nodes;

namespace AST.Visitors
{
    public interface IContractVisitor
    {
        public void Visit(ContractNode node);
    }
}
