using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class ScopeResolutionNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public IdentifierNode Scope { get; set; }
        public INode Property { get; set; }
        public TypeReferenceNode? ResultType { get; set; }
        public ExpressionType ExpressionType => ExpressionType.PropAccess;
        public INode Root => Utils.GetRoot(this);
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public ScopeResolutionNode(IdentifierNode scope, INode property, INode? parent = null)
        {
            Scope = scope;
            Property = property;
            Parent = parent;
            Type = NodeType.PropAccess;
            Meta = property.Meta;
        }

        public void Accept(IScopeResolutionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}