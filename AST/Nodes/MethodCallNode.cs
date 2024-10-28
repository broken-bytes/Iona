using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class MethodCallNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public INode Object { get; set; }
        public IdentifierNode Target { get; set; }
        public List<FuncCallArg> Args { get; set; } = new List<FuncCallArg>();
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }
        public ExpressionType ExpressionType => ExpressionType.FunctionCall;
        public INode? ResultType { get; set; }

        public MethodCallNode(INode objc, IdentifierNode target, INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.FuncCall;
            Object = objc;
            Target = target;
        }

        public void Accept(IMethodCallVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
