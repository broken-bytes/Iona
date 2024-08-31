using Lexer;
using Parser;

namespace Compiler
{
    public static class CompilerFactory
    {
        public static ICompiler Create()
        {
            var lexer = LexerFactory.Create();
            var parser = ParserFactory.Create();

            return new Compiler(lexer, parser);
        }
    }
}
