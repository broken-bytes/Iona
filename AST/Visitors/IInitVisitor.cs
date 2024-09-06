using AST.Nodes;

namespace AST.Visitors
{
    public interface IInitVisitor
    {
        public void Visit(InitNode node);
    }
}
