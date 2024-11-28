using AST.Types;

namespace AST.Visitors;

public interface IParameterVisitor
{
    public void Visit(ParameterNode parameter)
    {
        parameter.Accept(this);
    }
}