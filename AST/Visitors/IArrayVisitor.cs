using AST.Nodes;

namespace AST.Visitors
{
    public interface IArrayVisitor
    {
        public void Visit(ArrayNode node);
    }
}
