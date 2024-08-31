using AST.Nodes;

namespace AST.Visitors
{
    public interface IFileVisitor
    {
        public void Visit(FileNode node);
    }
}
