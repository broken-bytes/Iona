using AST.Types;

namespace AST.Nodes
{
    public interface IExpressionNode : INode
    {
        public ExpressionType ExpressionType { get; }
        public ITypeNode? ResultType { get; set; }
    }
}
