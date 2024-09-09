using AST.Nodes;
using AST.Types;
using Lexer.Tokens;
using Parser.Parsers;
using System.IO;

namespace Parser
{
    public interface IParser
    {
        public INode Parse(TokenStream stream);
    }
}
