//|--- ITypeck.cs ----------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using System.Reflection;
using AST.Nodes;
using Symbols;
using Symbols.Symbols;

namespace Typeck
{
    public interface ITypeck
    {
        public void DoSemanticAnalysis(List<FileNode> files, string assembly, SymbolTable symbolTable);
        public void AddImportedAssemblySymbols(SymbolTable table, List<string> assemblies);
    }
}
