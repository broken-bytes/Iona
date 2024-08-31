using Parser;

namespace Compiler
{
    public static class CompilerFactory
    {
        public static ICompiler Create()
        {
            var parser = ParserFactory.Create();

            return new Compiler(parser);
        }
    }
}
