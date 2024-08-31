using AST.Nodes;

namespace AST.Visitors
{
    public interface IEnumTypeVisitor
    {
        public void Visit(EnumTypeNode node);
    }
}
