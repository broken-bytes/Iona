using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class FuncCallNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public IdentifierNode Target { get; set; }
        public List<FuncCallArg> Args { get; set; } = new List<FuncCallArg>();
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }
        public ExpressionType ExpressionType => ExpressionType.FunctionCall;
        public ITypeReferenceNode? ResultType { get; set; }

        public FuncCallNode(IdentifierNode target, INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.FuncCall;
            Target = target;
        }

        public void Accept(IFuncCallVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
