using AST.Types;

namespace AST.Nodes
{
    public interface ITypeNode : INode
    {
        public string FullyQualifiedName { get; set; }
        public string Name { get; set; }
        public List<GenericArgument> GenericArguments { get; set; }
        public bool IsGeneric => GenericArguments.Count > 0;
    }
}
