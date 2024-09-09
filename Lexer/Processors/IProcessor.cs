using Lexer.Tokens;

namespace Lexer.Processors
{
    public interface IProcessor
    {
        Token? Process(string source);
    }
}
