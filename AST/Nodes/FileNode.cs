using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class FileNode : INode
    {
        public string Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public List<INode> Children { get; set; } = new List<INode>();
        public FileNode Root => Utils.GetRoot(this);
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public FileNode(string name, INode? parent = null)
        {
            Name = name;
            Parent = parent;
            Type = NodeType.File;
        }

        public void AddChild(INode child)
        {
            Children.Add(child);
        }

        public void Accept(IFileVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
