using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class ObjectLiteralNode : IExpressionNode
    {
        public struct Argument
        {
            public string Name;
            public IExpressionNode Value;
        }

        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public TypeReferenceNode ObjectType { get; set; }
        public List<Argument> Arguments { get; set; } = new List<Argument>();
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }
        public ExpressionType ExpressionType => ExpressionType.ObjectLiteral;
        public TypeReferenceNode? ResultType => ObjectType;

        public ObjectLiteralNode(TypeReferenceNode objectType, INode? parent)
        {
            Parent = parent;
            Type = NodeType.ObjectLiteral;
            ObjectType = objectType;
        }

        public void Accept(IObjectLiteralVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
