using AST.Nodes;

namespace AST.Visitors
{
    public interface IMethodCallVisitor
    {
        public void Visit(MethodCallNode node);
    }
}
