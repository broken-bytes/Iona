using AST.Nodes;

namespace Parser
{
    public interface IParser
    {
        public INode Parse(string source);
    }
}
