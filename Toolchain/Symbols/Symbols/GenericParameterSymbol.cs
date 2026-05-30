//|--- GenericParameterSymbol.cs -------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace Symbols.Symbols
{
    public class GenericParameterSymbol(string name) : ISymbol
    {
        public string Name { get; set; } = name;
        public List<ISymbol> Symbols { get; set; } = [];
        public Dictionary<string, List<ISymbol>> SymbolsByName { get; set; } = new Dictionary<string, List<ISymbol>>();
        public SymbolKind Kind { get; set; } = SymbolKind.GenericParameter;
        public ISymbol? Parent { get; set; }
        // Bounds declared via `#over<T: A & B>` or `where T: A & B`. The supplied type at
        // each call site must satisfy ALL entries (intersection / logical AND).
        public List<TypeSymbol> Constraints { get; set; } = new();
    }
}