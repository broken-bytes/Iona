﻿using AST.Nodes;
using Symbols;

namespace Typeck.Passes.Impl;

public class ImplPass : ISemanticAnalysisPass
{
    private readonly List<ISemanticAnalysisPass> _passes;

    internal ImplPass(List<ISemanticAnalysisPass> passes)
    {
        _passes = passes;
    }
    
    public void Run(List<FileNode> files, SymbolTable table, string assemblyName)
    {
        foreach (var pass in _passes)
        {
            pass.Run(files, table, assemblyName);
        }
    }
}
