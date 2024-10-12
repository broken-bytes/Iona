﻿namespace Symbols.Symbols
{
    public class TypeSymbol : ISymbol
    {
        public string Name { get; set; }
        public TypeKind TypeKind { get; }
        public List<ISymbol> Symbols { get; set; }
        public SymbolKind Kind { get; set; } = SymbolKind.Type;
        public ISymbol? Parent { get; set; }

        public TypeSymbol(string name, TypeKind kind)
        {
            Name = name;
            TypeKind = kind;
            Symbols = new List<ISymbol>();
        }

        public ISymbol? FindMember(string name) => 
            Symbols.First(child => child is BlockSymbol).Symbols.Find(symbol => symbol.Name == name);
    }
}
