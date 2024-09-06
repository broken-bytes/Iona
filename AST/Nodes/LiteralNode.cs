using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class LiteralNode: IExpressionNode
    {
        public string Value { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public ExpressionType ExpressionType => ExpressionType.Literal;
        public Types.Type? ResultType { get; set; }
        public LiteralType LiteralType { get; set; }

        public LiteralNode(string value, LiteralType literalType, INode? parent = null)
        {
            Value = value;
            Parent = parent;
            Type = NodeType.Literal;
            LiteralType = literalType;
            Value = value;
        }

        public void Accept(ILiteralVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
