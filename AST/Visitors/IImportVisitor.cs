using AST.Nodes;

namespace AST.Visitors
{
    public interface IImportVisitor
    {
        public void Visit(ImportNode import);
    }
}
