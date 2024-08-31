using Lexer.Processors;

namespace Lexer
{
    public static class LexerFactory
    {
        public static ILexer Create()
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

            return new Lexer(processors);
        }
    }
}
