﻿using Parser.Parsers;

namespace Parser
{
    public static class ParserFactory
    {
        public static IParser Create()
        {
            var typeParser = new TypeParser();
            var expressionParser = new ExpressionParser();
            var propertyParser = new PropertyParser(expressionParser, typeParser);
            var variableParser = new VariableParser(expressionParser);
            var funcParser = new FuncParser(expressionParser, variableParser, typeParser);
            var initParser = new InitParser(variableParser, typeParser);
            var classParser = new ClassParser(funcParser, initParser, propertyParser, typeParser);
            var contractParser = new ContractParser(funcParser, propertyParser, typeParser);
            var structParser = new StructParser(funcParser, initParser,propertyParser, typeParser);
            var moduleParser = new ModuleParser(classParser, contractParser, funcParser, variableParser, structParser);

            return new Parser(moduleParser);
        }
    }
}
