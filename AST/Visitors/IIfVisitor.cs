using AST.Nodes;

namespace AST.Visitors
{
    public interface IIfVisitor
    {
        public void Visit(IfNode node);
    }
}
