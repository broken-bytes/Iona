using AST.Nodes;

namespace ASTLogger
{
    public interface IASTLogger
    {
        public void Log(INode node);
    }
}
