using AST.Types;
using AST.Visitors;
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
        public INode ObjectType { get; set; }
        public List<Argument> Arguments { get; set; } = new List<Argument>();
        public Metadata Meta { get; set; }
        public ExpressionType ExpressionType => ExpressionType.ObjectLiteral;
        public INode? ResultType => ObjectType;

        public ObjectLiteralNode(INode objectType, INode? parent)
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
