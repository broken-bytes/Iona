using AST.Nodes;

namespace AST.Visitors
{
    public interface IUnaryExpressionVisitor
    {
        public void Visit(UnaryExpressionNode node);
    }
}
