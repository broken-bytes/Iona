using AST.Nodes;

namespace AST.Types
{
    public class GenericArgument : INode
    {
        public string Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public INode.Metadata Meta { get; set; }

        public GenericArgument(string name, INode? parent = null)
        {
            Name = name;
            Parent = parent;
            Type = NodeType.GenericType;
        }
    }
}
