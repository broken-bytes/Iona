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
        public IType? ResultType { get; set; }

        public ArrayLiteralNode()
        {
            Type = NodeType.ArrayLiteral;
        }

        public void Accept(IArrayLiteralVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
