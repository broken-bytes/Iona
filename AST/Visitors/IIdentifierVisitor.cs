using AST.Nodes;

namespace AST.Visitors
{
    public interface IIdentifierVisitor
    {
        public void Visit(IdentifierNode node);
    }
}
