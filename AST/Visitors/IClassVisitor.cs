using AST.Nodes;

namespace AST.Visitors
{
    public interface IClassVisitor
    {
        public void Visit(ClassNode node);
    }
}
