using AST.Nodes;

namespace AST.Visitors
{
    public interface ISelfVisitor
    {
        public void Visit(SelfNode node);
    }
}
