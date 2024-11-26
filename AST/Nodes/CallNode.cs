using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public interface ICallNode : IExpressionNode
    {
        public IdentifierNode Target { get; set; }
        public List<FuncCallArg> Args { get; set; }
    }
}