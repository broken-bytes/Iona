using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public  class FuncCallNode : INode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public INode Target { get; set; }
        public List<FuncCallArg> Args { get; set; }

        public FuncCallNode(INode target, List<FuncCallArg> args, INode? parent)
        {
            Parent = parent;
            Type = NodeType.FuncCall;
            Target = target;
            Args = args;
        }

        public void Accept(IFuncCallVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
