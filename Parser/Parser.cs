using AST.Nodes;
using Lexer.Tokens;
using Parser.Parsers;

namespace Parser
{
    public class Parser : IParser
    {
        private ExpressionParser expressionParser;
        private ModuleParser moduleParser;
        private VariableParser variableParser;

        internal Parser(ExpressionParser expressionParser, ModuleParser moduleParser, VariableParser variableParser)
        {
            this.expressionParser = expressionParser;
            this.moduleParser = moduleParser;
            this.variableParser = variableParser;
        }

        public INode Parse(TokenStream tokens)
        {
            throw new System.NotImplementedException();
        }
    }
}
