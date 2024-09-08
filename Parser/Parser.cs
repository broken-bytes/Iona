using AST.Nodes;
using Lexer.Tokens;
using Parser.Parsers;

namespace Parser
{
    public class Parser : IParser
    {
        private ModuleParser moduleParser;

        internal Parser(ModuleParser moduleParser)
        {
            this.moduleParser = moduleParser;
        }

        public INode Parse(TokenStream tokens, INode? parent = null)
        {
            // Every file should start with a module declaration, thus we can directly use the module parser
            return moduleParser.Parse(tokens, null);
        }
    }
}
