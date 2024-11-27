using AST.Nodes;

namespace AST.Visitors;

public interface IEnumCaseAccessVisitor
{
    public void Visit(EnumCaseAccessNode node);
}