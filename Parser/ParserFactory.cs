using Parser.Parsers;

namespace Parser
{
    public static class ParserFactory
    {
        public static IParser Create()
        {
            var typeParser = new TypeParser();
            var expressionParser = new ExpressionParser();
            var propertyParser = new PropertyParser(expressionParser);
            var variableParser = new VariableParser(expressionParser);
            var funcParser = new FuncParser(variableParser, typeParser);
            var classParser = new ClassParser(funcParser, propertyParser, typeParser);
            var contractParser = new ContractParser(funcParser, propertyParser, typeParser);
            var structParser = new StructParser(funcParser, propertyParser, typeParser);
            var moduleParser = new ModuleParser(classParser, contractParser, funcParser, variableParser, structParser);

            return new Parser(moduleParser);
        }
    }
}
