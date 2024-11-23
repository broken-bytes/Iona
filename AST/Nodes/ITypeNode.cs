using AST.Types;

namespace AST.Nodes
{
    public interface ITypeNode : INode
    {
        public string FullyQualifiedName { get; set; }
        public string Name { get; set; }
        public List<GenericParameter> GenericArguments { get; set; }
        public bool IsGeneric => GenericArguments.Count > 0;
        public BlockNode? Body { get; set; }
    }
}
