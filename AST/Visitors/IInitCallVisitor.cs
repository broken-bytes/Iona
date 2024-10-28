using AST.Nodes;

namespace AST.Visitors
{
    public interface IInitCallVisitor
    {
        public void Visit(InitCallNode node);
    }
}
