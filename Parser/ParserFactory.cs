using Parser.Parsers;

namespace Parser
{
    public static class ParserFactory
    {
        public static IParser Create()
        {
            var accessLevelParser = new AccessLevelParser();
            var genericArgsParser = new GenericArgsParser();
            var typeParser = new TypeParser();
            var expressionParser = new ExpressionParser();
            var propertyParser = new PropertyParser(accessLevelParser, expressionParser, typeParser);
            var variableParser = new VariableParser(expressionParser);
            var funcParser = new FuncParser(accessLevelParser, typeParser);
            var initParser = new InitParser(accessLevelParser, typeParser);
            var classParser = new ClassParser(accessLevelParser, genericArgsParser, typeParser);
            var contractParser = new ContractParser(accessLevelParser, genericArgsParser, typeParser);
            var structParser = new StructParser(accessLevelParser, genericArgsParser, typeParser);
            var moduleParser = new ModuleParser();
            var statementParser = new StatementParser(
                classParser,
                contractParser,
                expressionParser,
                funcParser,
                initParser,
                moduleParser,
                propertyParser,
                structParser,
                variableParser
            );

            classParser.Setup(statementParser);
            contractParser.Setup(statementParser);
            funcParser.Setup(statementParser);
            initParser.Setup(statementParser);
            moduleParser.Setup(statementParser);
            propertyParser.Setup(statementParser);
            structParser.Setup(statementParser);

            return new Parser(statementParser);
        }
    }
}
