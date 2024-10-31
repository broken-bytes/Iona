using AST.Types;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class GenericTypeReferenceNode : ITypeReferenceNode
    {
        public string FullyQualifiedName => $"{Module}.{Name}";
        public string Name { get; set; }
        public string Module { get; set; }
        public Kind TypeKind { get; set; }
        public List<ITypeReferenceNode> GenericArguments { get; set; } = new List<ITypeReferenceNode>();
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }
        public INode Root => Utils.GetRoot(this);
        public TypeReferenceKind ReferenceKind { get; set; }

        public GenericTypeReferenceNode(string name)
        {
            Name = name;
            Module = "";
            TypeKind = Kind.Unknown;
            ReferenceKind = TypeReferenceKind.Generic;
        }

        public GenericTypeReferenceNode(string name, string module, List<ITypeReferenceNode> genericArguments, Kind kind = Kind.Unknown)
        {
            Name = name;
            Module = module;
            GenericArguments = genericArguments;
            TypeKind = kind;
        }

        public override string ToString()
        {
            return $"{Name}<{string.Join(", ", GenericArguments)}>";
        }
    }
}
