using Lexer;
using Parser;

namespace Compiler
{
    public class Compiler : ICompiler
    {
        private readonly ILexer lexer;
        private readonly IParser parser;

        internal Compiler(ILexer lexer, IParser parser)
        {
            this.lexer = lexer;
            this.parser = parser;
        }

        public void Compile(string source, string filename)
        {
            var tokens = lexer.Tokenize(source, filename);
            var ast = parser.Parse(tokens);
            Console.WriteLine(ast);
            // Do something with the AST
        }
    }
}
