using AST.Nodes;

namespace AST.Visitors
{
    public interface IWhileVisitor
    {
        public void Visit(WhileNode node);
    }
}
