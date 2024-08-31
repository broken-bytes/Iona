using AST.Nodes;

namespace AST.Visitors
{
    public interface IClassTypeVisitor
    {
        public void Visit(ClassTypeNode node);
    }
}
