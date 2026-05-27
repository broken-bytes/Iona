//|--- EnumCaseSymbol.cs ---------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Nodes;

namespace Symbols.Symbols;

public class EnumCaseSymbol : ISymbol
{
    public string Name { get; set; }
    public string CsharpName { get; set; }
    
    public SymbolKind Kind { get; set; } = SymbolKind.EnumCase;
    public ISymbol? Parent { get; set; }
    public List<ISymbol> Symbols { get; set; } = [];
    public Dictionary<string, List<ISymbol>> SymbolsByName { get; set; } = new Dictionary<string, List<ISymbol>>();

    public EnumCaseSymbol(string name, string csharpName)
    {
        Name = name;
        CsharpName = csharpName;
    }
}