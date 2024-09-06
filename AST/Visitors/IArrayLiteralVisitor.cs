using AST.Nodes;

namespace AST.Visitors
{
    public interface IArrayLiteralVisitor
    {
        public void Visit(ArrayLiteralNode node);
    }
}
