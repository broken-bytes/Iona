using AST.Nodes;

namespace AST.Visitors
{
    public interface IPropertyVisitor
    {
        public void Visit(PropertyNode node);
    }
}
