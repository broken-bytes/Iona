using Generator;
using Lexer;
using Parser;
using Typeck;
using Shared;

namespace Compiler
{
    public static class CompilerFactory
    {
        public static ICompiler Create()
        {
            var errorCollector = ErrorCollectorFactory.Create();
            var generator = GeneratorFactory.Create(errorCollector);
            var lexer = LexerFactory.Create(errorCollector);
            var parser = ParserFactory.Create(errorCollector);
            var typeck = TypeckFactory.Create(errorCollector);

            return new Compiler(lexer, parser, typeck, generator, errorCollector);
        }
    }
}
