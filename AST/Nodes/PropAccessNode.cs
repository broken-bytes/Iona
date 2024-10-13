using AST.Types;
using AST.Visitors;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class PropAccessNode : INode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public string Name { get; set; }
        public INode Root => Utils.GetRoot(this);
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public PropAccessNode(IdentifierNode identifier, INode? parent = null)
        {
            Name = identifier.Name;
            Parent = parent;
            Type = NodeType.PropAccess;
            Meta = identifier.Meta;
        }

        public void Accept(IPropAccessVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
