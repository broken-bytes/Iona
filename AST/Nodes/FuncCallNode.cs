using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public  class FuncCallNode : INode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public IExpressionNode Target { get; set; }
        public List<FuncCallArg> Args { get; set; } = new List<FuncCallArg>();

        public FuncCallNode(IExpressionNode target, INode? parent = null)
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
