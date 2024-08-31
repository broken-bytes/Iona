using AST.Types;

namespace AST.Nodes
{
    public interface INode
    {
        string Name { get; set; }
        string Module { get; set; }
        INode Parent { get; set; }
        NodeType Type { get; set; }
        INode Root { get; }
    }
}
