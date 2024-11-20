using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class PropAccessNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Object { get; set; }
        public INode Property { get; set; }
        public TypeReferenceNode? ResultType { get; set; }
        public ExpressionType ExpressionType => ExpressionType.PropAccess;
        public FileNode Root => Utils.GetRoot(this);
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public PropAccessNode(INode obj, INode property, INode? parent = null)
        {
            Object = obj;
            Property = property;
            Parent = parent;
            Type = NodeType.PropAccess;
            Meta = property.Meta;
        }

        public void Accept(IPropAccessVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
