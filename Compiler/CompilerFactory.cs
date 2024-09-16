using Generator;
using Lexer;
using Parser;
using Typeck;

namespace Compiler
{
    public static class CompilerFactory
    {
        public static ICompiler Create()
        {
            var generator = GeneratorFactory.Create();
            var lexer = LexerFactory.Create();
            var parser = ParserFactory.Create();
            var typeck = TypeckFactory.Create();

            return new Compiler(lexer, parser, typeck, generator);
        }
    }
}
