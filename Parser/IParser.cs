using AST.Nodes;
using Lexer.Tokens;

namespace Parser
{
    public interface IParser
    {
        public INode Parse(TokenStream stream, string assemblyName);
    }
}
