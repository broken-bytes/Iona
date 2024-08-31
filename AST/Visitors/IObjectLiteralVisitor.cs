using AST.Nodes;

namespace AST.Visitors
{
    public interface IObjectLiteralVisitor
    {
        public void Visit(ObjectLiteralNode node);
    }
}
