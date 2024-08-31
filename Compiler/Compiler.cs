using Parser;

namespace Compiler
{
    public class Compiler : ICompiler
    {
        private readonly IParser parser;

        internal Compiler(IParser parser)
        {
            this.parser = parser;
        }

        public void Compile(string source)
        {
            var ast = parser.Parse(source);
            Console.WriteLine(ast);
            // Do something with the AST
        }
    }
}
