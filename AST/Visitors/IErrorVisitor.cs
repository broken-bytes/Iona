using AST.Nodes;

namespace AST.Visitors
{
    public interface IErrorVisitor
    {
        public void Visit(ErrorNode node);
    }
}
