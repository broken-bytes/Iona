using AST.Types;

namespace AST.Nodes
{
    public interface IAccessLevelNode: INode
    {
        AccessLevel AccessLevel { get; set; }
    }
}
