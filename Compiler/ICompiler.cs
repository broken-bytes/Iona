namespace Compiler
{
    public interface ICompiler
    {
        public bool Compile(
            string assemblyName,
            List<CompilationUnit> files,
            bool intermediate,
            bool debug,
            bool emitIr,
            string emitIrJsonDir,
            List<string> assemblyPaths,
            List<string> assemblyRefs,
            string targetFramework,
            string outputType
            );
    }
}
