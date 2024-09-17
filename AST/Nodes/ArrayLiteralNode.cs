using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class ArrayLiteralNode : IExpressionNode
    {
        public List<IExpressionNode> Values { get; set; } = new List<IExpressionNode>();
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public ExpressionType ExpressionType => ExpressionType.Literal;
        public INode? ResultType { get; set; }

        public ArrayLiteralNode(INode? parent = null)
        {
            Type = NodeType.ArrayLiteral;
            Parent = parent;
        }

        public void Accept(IArrayLiteralVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
