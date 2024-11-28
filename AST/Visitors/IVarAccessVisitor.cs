using AST.Nodes;

namespace AST.Visitors;

public interface IVarAccessVisitor
{
    public void Visit(VarAccessNode node);
}