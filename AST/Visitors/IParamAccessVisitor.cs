using AST.Nodes;
using AST.Types;

namespace AST.Visitors;

public interface IParamAccessVisitor
{
    public void Visit(ParamAccessNode node);
}