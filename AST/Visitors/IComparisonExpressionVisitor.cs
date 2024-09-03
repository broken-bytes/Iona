using AST.Nodes;

namespace AST.Visitors
{
    public interface IComparisonExpressionVisitor
    {
        public void Visit(ComparisonExpressionNode node);
    }
}
