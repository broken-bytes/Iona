using AST.Nodes;

namespace AST.Visitors
{
    public interface IGuardVisitor
    {
        public void Visit(GuardNode node);
    }
}
