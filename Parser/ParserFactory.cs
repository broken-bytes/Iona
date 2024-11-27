using Parser.Parsers;
using Parser.Parsers;
using Shared;

namespace Parser
{
    public static class ParserFactory
    {
        public static IParser Create(
            IErrorCollector errorCollector,
            IWarningCollector warningCollector,
            IFixItCollector fixItCollector
        )
        {
            var accessLevelParser = new AccessLevelParser();
            var genericArgsParser = new GenericArgsParser();
            var typeParser = new TypeParser();
            var funcCallParser = new FuncCallParser(errorCollector);
            var memberAccessParser = new MemberAccessParser();
            var scopeResolutionParser = new ScopeResolutionParser();
            var expressionParser = new ExpressionParser(
                funcCallParser, 
                memberAccessParser, 
                scopeResolutionParser, 
                typeParser, 
                errorCollector
            );
            var propertyParser = new PropertyParser(accessLevelParser, expressionParser, typeParser);
            var variableParser = new VariableParser(expressionParser);
            var blockParser = new BlockParser();
            var funcParser = new FuncParser(accessLevelParser, blockParser, typeParser, errorCollector);
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
                memberAccessParser,
                moduleParser,
                operatorParser,
                propertyParser,
                structParser,
                variableParser,
                errorCollector
            );

            classParser.Setup(statementParser);
            contractParser.Setup(statementParser);
            blockParser.Setup(expressionParser, memberAccessParser, statementParser);
            funcCallParser.Setup(expressionParser);
            funcParser.Setup(expressionParser, statementParser);
            initParser.Setup(statementParser);
            memberAccessParser.Setup(expressionParser, funcCallParser, statementParser);
            scopeResolutionParser.Setup(expressionParser, funcCallParser, statementParser);
            moduleParser.Setup(statementParser);
            operatorParser.Setup(expressionParser, statementParser);
            propertyParser.Setup(statementParser);
            structParser.Setup(statementParser);

            return new Parser(statementParser, errorCollector);
        }
    }
}
