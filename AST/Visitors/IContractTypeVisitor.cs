using AST.Nodes;

namespace AST.Visitors
{
    public interface IContractTypeVisitor
    {
        public void Visit(ContractTypeNode node);
    }
}
