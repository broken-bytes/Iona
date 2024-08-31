using Lexer.Processors;
using Lexer.Tokens;

namespace Lexer
{
    public interface ILexer
    {
        public TokenStream Tokenize(string code, string fileName);
    }
}
