using AST.Nodes;

namespace AST.Visitors
{
    public interface IStructVisitor
    {
        public void Visit(StructNode node);
    }
}
