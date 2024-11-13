using AST.Nodes;

namespace AST.Visitors
{
    public interface IScopeResolutionVisitor
    {
        public void Visit(ScopeResolutionNode node);
    }
}