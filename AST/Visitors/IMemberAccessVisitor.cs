using AST.Nodes;

namespace AST.Visitors
{
    public interface IMemberAccessVisitor
    {
        public void Visit(MemberAccessExpressionNode node);
    }
}
