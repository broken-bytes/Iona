using AST.Nodes;

namespace AST.Visitors
{
    public interface IEnumVisitor
    {
        public void Visit(EnumNode node);
    }
}
