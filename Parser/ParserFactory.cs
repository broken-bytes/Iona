using Parser.Parsers;
using Parser.Parsers.Parser.Parsers;

namespace Parser
{
    public static class ParserFactory
    {
        public static IParser Create()
        {
            var accessLevelParser = new AccessLevelParser();
            var genericArgsParser = new GenericArgsParser();
            var typeParser = new TypeParser();
            var funcCallParser = new FuncCallParser();
            var memberAccessParser = new MemberAccessParser();
            var expressionParser = new ExpressionParser(funcCallParser, memberAccessParser, typeParser);
            var propertyParser = new PropertyParser(accessLevelParser, expressionParser, typeParser);
            var variableParser = new VariableParser(expressionParser);
            var blockParser = new BlockParser();
            var funcParser = new FuncParser(accessLevelParser, blockParser, typeParser);
            var initParser = new InitParser(accessLevelParser, blockParser, typeParser);
            var classParser = new ClassParser(accessLevelParser, genericArgsParser, typeParser);
            var contractParser = new ContractParser(accessLevelParser, genericArgsParser, typeParser);
            var structParser = new StructParser(accessLevelParser, genericArgsParser, typeParser);
            var moduleParser = new ModuleParser();
            var operatorParser = new OperatorParser(accessLevelParser, blockParser, typeParser);
            var statementParser = new StatementParser(
                classParser,
                contractParser,
                expressionParser,
                funcParser,
                initParser,
                moduleParser,
                operatorParser,
                propertyParser,
                structParser,
                variableParser
            );

            classParser.Setup(statementParser);
            contractParser.Setup(statementParser);
            blockParser.Setup(expressionParser, memberAccessParser, statementParser);
            funcCallParser.Setup(expressionParser);
            funcParser.Setup(expressionParser, statementParser);
            initParser.Setup(statementParser);
            memberAccessParser.Setup(expressionParser, statementParser);
            moduleParser.Setup(statementParser);
            operatorParser.Setup(expressionParser, statementParser);
            propertyParser.Setup(statementParser);
            structParser.Setup(statementParser);

            return new Parser(statementParser);
        }
    }
}
