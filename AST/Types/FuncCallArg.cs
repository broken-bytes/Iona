using AST.Nodes;

namespace AST.Types
{
    public class FuncCallArg(string name, IExpressionNode value)
    {
        public string Name = name;
        public IExpressionNode Value = value;
    }
}
