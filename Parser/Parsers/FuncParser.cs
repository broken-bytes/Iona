using AST.Nodes;

namespace Parser.Parsers
{
    public  class FuncParser : IParser
    {
        VariableParser variableParser;

        internal FuncParser(VariableParser variableParser)
        {
            this.variableParser = variableParser;
        }

        public INode Parse(Lexer.Tokens.TokenStream tokens)
        {
            throw new System.NotImplementedException();
        }
    }
}
