using AST.Nodes;

namespace AST.Visitors
{
    public interface IVariableVisitor
    {
        public void Visit(VariableNode node);
    }
}
