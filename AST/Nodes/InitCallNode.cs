using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class InitCallNode : ICallNode
    {
        public string TypeFullName { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public IdentifierNode Target { get; set; }
        public List<FuncCallArg> Args { get; set; } = new List<FuncCallArg>();
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }
        public ExpressionType ExpressionType => ExpressionType.FunctionCall;
        public TypeReferenceNode? ResultType { get; set; }

        public InitCallNode(string typeFQN, INode? parent = null)
        {
            TypeFullName = typeFQN;
            Type = NodeType.FuncCall;
            Parent = parent;
        }

        public void Accept(IInitCallVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
