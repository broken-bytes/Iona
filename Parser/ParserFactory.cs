using Lexer;

namespace Parser
{
    public static class ParserFactory
    {
        public static IParser Create()
        {
            var lexer = LexerFactory.Create();

            return new Parser(lexer);
        }
    }
}
