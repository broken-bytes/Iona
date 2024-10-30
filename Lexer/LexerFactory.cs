using Lexer.Processors;
using Shared;

namespace Lexer
{
    public static class LexerFactory
    {
        public static ILexer Create(
            IErrorCollector errorCollector,
            IWarningCollector warningCollector,
            IFixItCollector fixItCollector
        )
        {
            List<IProcessor> processors = new List<IProcessor>();
            var numberProcessor = new NumberProcessor();
            processors.Add(new ControlFlowProcessor());
            processors.Add(new GroupingProcessor());
            processors.Add(new KeywordProcessor());
            processors.Add(new OperatorProcessor());
            processors.Add(new PunctuationProcessor());
            processors.Add(new SpecialProcessor());
            processors.Add(new IdentifierProcessor());
            processors.Add(new LiteralProcessor(numberProcessor));

            return new Lexer(processors, errorCollector, warningCollector, fixItCollector);
        }
    }
}
