using AST.Nodes;

namespace AST.Visitors
{
    public interface IReturnVisitor
    {
        public void Visit(ReturnNode node);
    }
}
