using AST.Nodes;

namespace AST.Visitors
{
    public interface IStructTypeVisitor
    {
        public void Visit(StructTypeNode node);
    }
}
