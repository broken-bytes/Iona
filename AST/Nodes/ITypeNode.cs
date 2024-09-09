using AST.Types;

namespace AST.Nodes
{
    public interface ITypeNode : INode
    {
        public List<GenericArgument> GenericArguments { get; set; }
        public bool IsGeneric => GenericArguments.Count > 0;
    }
}
