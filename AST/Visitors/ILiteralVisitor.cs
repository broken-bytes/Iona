using AST.Nodes;

namespace AST.Visitors
{
    public interface ILiteralVisitor
    {
        public void Visit(LiteralNode node);
    }
}
