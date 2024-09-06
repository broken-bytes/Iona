using Parser.Parsers;

namespace Parser
{
    public static class ParserFactory
    {
        public static IParser Create()
        {
            var expressionParser = new ExpressionParser();
            var propertyParser = new PropertyParser(expressionParser);
            var variableParser = new VariableParser(expressionParser);
            var funcParser = new FuncParser(variableParser);
            var contractParser = new ContractParser(funcParser, propertyParser);
            var moduleParser = new ModuleParser(contractParser, funcParser, variableParser);

            return new Parser(moduleParser);
        }
    }
}
