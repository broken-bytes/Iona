using AST.Nodes;

namespace AST.Visitors;

public interface IEnumCaseVisitor
{
    public void Visit(EnumCaseNode node);
}