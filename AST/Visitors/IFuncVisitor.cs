using AST.Nodes;

namespace AST.Visitors
{
    public interface IFuncVisitor
    {
        public void Visit(FuncNode node);
    }
}
