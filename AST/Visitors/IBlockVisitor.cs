using AST.Nodes;

namespace AST.Visitors
{
    public interface IBlockVisitor
    {
        public void Visit(BlockNode node);
    }
}
