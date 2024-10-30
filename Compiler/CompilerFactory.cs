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
            var warningCollector = WarningCollectorFactory.Create();
            var fixitCollector = FixItCollectorFactory.Create();
            var generator = GeneratorFactory.Create(errorCollector, warningCollector, fixitCollector);
            var lexer = LexerFactory.Create(errorCollector, warningCollector, fixitCollector);
            var parser = ParserFactory.Create(errorCollector, warningCollector, fixitCollector);
            var typeck = TypeckFactory.Create(errorCollector, warningCollector, fixitCollector);

            return new Compiler(lexer, parser, typeck, generator, errorCollector, warningCollector, fixitCollector);
        }
    }
}
