using Parser.Parsers;

namespace Parser
{
    public static class ParserFactory
    {
        public static IParser Create()
        {
            var expressionParser = new ExpressionParser();
            var moduleParser = new ModuleParser();
            var variableParser = new VariableParser(expressionParser);

            return new Parser(expressionParser, moduleParser, variableParser);
        }
    }
}
