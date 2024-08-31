using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class FileNode : INode
    {
        public string Name { get; set; }
        public string Module { get; set; }
         public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);

        public FileNode(string name, string module, INode? parent)
        {
            Name = name;
            Module = module;
            Parent = parent;
            Type = NodeType.File;
        }

        public void Accept(IFileVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
