using AST.Types;

namespace AST.Nodes
{
    public interface INode
    {
        INode? Parent { get; set; }
        NodeType Type { get; set; }
        INode Root { get; }
    }
}
