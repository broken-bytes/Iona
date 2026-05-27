//|--- DeclPass.cs ---------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Nodes;
using Symbols;

namespace Typeck.Passes;

public class DeclPass : ISemanticAnalysisPass
{
    private readonly List<ISemanticAnalysisPass> _subPasses;
    
    internal DeclPass(List<ISemanticAnalysisPass> subPasses)
    {
        _subPasses = subPasses;
    }
    
    public void Run(List<FileNode> files, SymbolTable table, string assemblyName)
    {
        foreach (var pass in _subPasses)
        {
            pass.Run(files, table, assemblyName);
        }
    }
}