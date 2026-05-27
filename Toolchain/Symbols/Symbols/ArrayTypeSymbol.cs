//|--- ArrayTypeSymbol.cs --------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace Symbols.Symbols
{
    public class ArrayTypeSymbol : ITypeSymbol
    {
        public string FullyQualifiedName => GetFullyQualifiedName(this);
        public string Name { get; set; }
        public bool IsArray => true;
        public bool IsConcrete => false;
        public bool IsGeneric => false;
        public ITypeSymbol ElementType { get; set; }
        public TypeKind TypeKind { get; }
        public List<ISymbol> Symbols { get; set; }
        public Dictionary<string, List<ISymbol>> SymbolsByName { get; set; }
        public SymbolKind Kind { get; set; } = SymbolKind.Type;
        public ISymbol? Parent { get; set; }

        public ArrayTypeSymbol(string name, ITypeSymbol element, TypeKind kind)
        {
            Name = name;
            ElementType = element;
            TypeKind = kind;
            Symbols = new List<ISymbol>();
            SymbolsByName = new Dictionary<string, List<ISymbol>>();
        }

        public ISymbol? FindMember(string name) =>
            Symbols.First(child => child is BlockSymbol).LookupSymbol(name);

        private static string GetFullyQualifiedName(ISymbol symbol)
        {
            var name = symbol.Name;
            var parent = symbol.Parent;

            while (parent != null)
            {
                name = $"{parent.Name}.{name}";
                parent = parent.Parent;
            }

            return name;
        }
    }
}
