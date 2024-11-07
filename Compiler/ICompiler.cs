namespace Compiler
{
    public interface ICompiler
    {
        public void Compile(string assemblyName, List<CompilationUnit> files, bool intermediate);
    }
}
