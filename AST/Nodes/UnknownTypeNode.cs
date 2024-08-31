using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class UnknownTypeNode : ITypeNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
         public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public NodeKind Kind { get; set; }

        public UnknownTypeNode(string name, string module, INode? parent)
        {
            Name = name;
            Module = module;
            Parent = parent;
            Type = NodeType.NominalType;
            Kind = NodeKind.UnknownKind;
        }

        public void Accept(IUnknownTypeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
