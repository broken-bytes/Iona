using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class ObjectLiteralNode : INode
    {
        public struct Argument
        {
            public string Name;
            public IExpressionNode Value;
        }

        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public IdentifierNode Target { get; set; }
        public List<Argument> Arguments { get; set; }

        public ObjectLiteralNode(IdentifierNode target, List<Argument> arguments, INode? parent)
        {
            Parent = parent;
            Type = NodeType.ObjectLiteral;
            Target = target;
            Arguments = arguments;
        }

        public void Accept(IObjectLiteralVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
