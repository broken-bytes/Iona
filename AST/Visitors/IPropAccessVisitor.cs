using AST.Nodes;

namespace AST.Visitors
{
    public interface IPropAccessVisitor
    {
        public void Visit(PropAccessNode node);
    }
}
