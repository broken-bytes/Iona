using Lexer.Tokens;

namespace Lexer.Processors
{
    public class PunctuationProcessor : IProcessor
    {
        public Token? Process(string source)
        {
            if (string.IsNullOrEmpty(source) || char.IsLetterOrDigit(source[0]))
            {
                return null;
            }

            // Check if the first character is a comma
            if (Utils.CheckMatchingSequence(source, Special.Comma.AsString()))
            {
                return Utils.MakeToken(TokenType.Comma, Special.Comma.AsString());
            }

            return null;
        }
    }
}
