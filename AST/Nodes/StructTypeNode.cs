using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class StructTypeNode : ITypeNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
         public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public NodeKind Kind { get; set; }

        public StructTypeNode(string name, string module, INode? parent)
        {
            Name = name;
            Module = module;
            Parent = parent;
            Type = NodeType.NominalType;
            Kind = NodeKind.Struct;
        }

        public void Accept(IStructTypeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
