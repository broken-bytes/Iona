using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class FileNode : INode
    {
        public string Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);

        public FileNode(string name, INode? parent)
        {
            Name = name;
            Parent = parent;
            Type = NodeType.File;
        }

        public void Accept(IFileVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
