using AST.Nodes;

namespace AST.Visitors
{
    public interface IBinaryExpressionVisitor
    {
        public void Visit(BinaryExpressionNode node);
    }
}
