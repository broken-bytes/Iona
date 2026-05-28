//|--- FuncSymbol.cs -------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Types;

namespace Symbols.Symbols
{
    public class FuncSymbol : ISymbol
    {
        public string Name { get; set; }
        public string CsharpName { get; set; }
        public TypeSymbol ReturnType { get; set; }
        public List<ISymbol> Symbols { get; set; }
        public Dictionary<string, List<ISymbol>> SymbolsByName { get; set; }
        public SymbolKind Kind { get; set; } = SymbolKind.Function;
        public ISymbol? Parent { get; set; }
        public AccessLevel AccessLevel { get; set; } = AccessLevel.Private;
        public bool IsMutating { get; set; }
        public bool IsAsync { get; set; }
        public bool IsOpen { get; set; }
        public bool IsOverride { get; set; }
        public string? CsharpOwnerFqn { get; set; }

        public FuncSymbol(string ionaName, string csharpName)
        {
            Name = ionaName;
            CsharpName = csharpName;
            ReturnType = new TypeSymbol("", TypeKind.Unknown);
            Symbols = new List<ISymbol>();
            SymbolsByName = new Dictionary<string, List<ISymbol>>();
        }
    }
}
