using AST.Nodes;
using Lexer;

namespace Parser
{
    public class Parser : IParser
    {
        private readonly ILexer lexer;

        internal Parser(ILexer lexer)
        {
            this.lexer = lexer;
        }

        public INode Parse(string source)
        {
            throw new System.NotImplementedException();
        }
    }
}
