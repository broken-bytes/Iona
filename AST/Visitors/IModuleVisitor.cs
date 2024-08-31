using AST.Nodes;

namespace AST.Visitors
{
    public interface IModuleVisitor
    {
        public void Visit(ModuleNode node);
    }
}
