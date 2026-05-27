//|--- IGenerator.cs -------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Nodes;
using Symbols;

namespace Generator
{
    public interface IGenerator
    {
        public Assembly CreateAssembly(string name, SymbolTable table);
    }
}
