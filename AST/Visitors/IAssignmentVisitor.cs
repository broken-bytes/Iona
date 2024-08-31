using AST.Nodes;

namespace AST.Visitors
{
    public interface IAssignmentVisitor
    {
        public void Visit(AssignmentNode node);
    }
}
