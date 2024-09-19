using AST.Types;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class GenericTypeReferenceNode : INode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public Kind TypeKind { get; set; }
        public List<INode> GenericArguments { get; set; } = new List<INode>();
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public Metadata Meta { get; set; }

        public INode Root => throw new NotImplementedException();

        public GenericTypeReferenceNode(string name)
        {
            Name = name;
            Module = "";
            TypeKind = Kind.Unknown;
        }

        public GenericTypeReferenceNode(string name, string module, List<INode> genericArguments, Kind kind = Kind.Unknown)
        {
            Name = name;
            Module = module;
            GenericArguments = genericArguments;
            TypeKind = kind;
        }
    }
}
