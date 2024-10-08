﻿namespace Symbols.Symbols
{
    public interface ISymbol
    {
        string Name { get; set; }
        public List<ISymbol> Symbols { get; set; }
        public SymbolKind Kind { get; set; }
        public ISymbol? Parent { get; set; }

        public void AddSymbol(ISymbol symbol)
        {
            Symbols.Add(symbol);
            symbol.Parent = this;
        }

        public ISymbol? LookupSymbol(string name)
        {
            foreach (var symbol in Symbols)
            {
                if (symbol.Name == name)
                {
                    return symbol;
                }
            }

            return null;
        }
    }
}
