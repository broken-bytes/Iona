using AST.Types;

namespace AST.Nodes
{
    public interface ITypeNode : INode
    {
        public NodeKind Kind { get; set; }
    }
}
