//|--- ICompiler.cs --------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

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
            string emitDiagnosticsJsonPath,
            List<string> assemblyPaths,
            List<string> assemblyRefs,
            string targetFramework,
            string outputType,
            string outputPath
            );
    }
}
