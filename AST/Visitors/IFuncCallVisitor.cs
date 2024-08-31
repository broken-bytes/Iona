using AST.Nodes;

namespace AST.Visitors
{
    public interface IFuncCallVisitor
    {
        public void Visit(FuncCallNode node);
    }
}
