using AST.Nodes;

namespace AST.Visitors
{
    public interface IUnknownTypeVisitor
    {
        public void Visit(UnknownTypeNode node);
    }
}
