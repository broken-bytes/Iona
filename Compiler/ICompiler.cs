namespace Compiler
{
    public interface ICompiler
    {
        public bool Compile(
            string assemblyName, 
            List<CompilationUnit> files, 
            bool intermediate, 
            bool debug,
            List<string> assemblyPaths,
            List<string> assemblyRefs
            );
    }
}
