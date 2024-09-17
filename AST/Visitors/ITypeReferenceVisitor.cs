using AST.Nodes;

namespace AST.Visitors
{
    public interface ITypeReferenceVisitor
    {
        public void Visit(TypeReferenceNode node);
    }
}
