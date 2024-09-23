using AST.Nodes;

namespace AST.Visitors
{
    public interface IOperatorVisitor
    {
        public void Visit(OperatorNode node);
    }
}
