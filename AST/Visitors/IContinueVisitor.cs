using AST.Nodes;

namespace AST.Visitors
{
    public interface IContinueVisitor
    {
        public void Visit(ContinueNode node);
    }
}
