using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class FuncCallNode : ICallNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public IdentifierNode Target { get; set; }
        public List<FuncCallArg> Args { get; set; } = [];
        public List<GenericArgument> GenericArgs { get; set; } = [];
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }
        public ExpressionType ExpressionType => ExpressionType.FunctionCall;
        public TypeReferenceNode? ResultType { get; set; }

        public FuncCallNode(IdentifierNode target, INode? parent = null)
        {
            Type = NodeType.FuncCall;
            Parent = parent;
            Target = target;
        }

        public override string ToString()
        {
            return Target.ToString();
        }

        public void Accept(IFuncCallVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
