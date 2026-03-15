using AST.Nodes;

namespace AST.Visitors
{
    public interface IForVisitor
    {
        public void Visit(ForNode node);
    }
}
