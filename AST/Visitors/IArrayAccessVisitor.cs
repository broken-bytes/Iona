using AST.Nodes;

namespace AST.Visitors
{
    public interface IArrayAccessVisitor
    {
        public void Visit(ArrayAccessNode node);
    }
}
