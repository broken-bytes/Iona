using AST.Nodes;

namespace AST.Visitors
{
    public interface IBreakVisitor
    {
        public void Visit(BreakNode node);
    }
}
