namespace Symbols.Symbols
{
    public interface ISymbol
    {
        string Name { get; set; }
        public List<ISymbol> Symbols { get; set; }
        public Dictionary<string, List<ISymbol>> SymbolsByName { get; set; }
        public SymbolKind Kind { get; set; }
        public ISymbol? Parent { get; set; }
    }

    public static class SymbolExtensions
    {
        public static void AddSymbol(this ISymbol self, ISymbol symbol)
        {
            self.Symbols.Add(symbol);
            symbol.Parent = self;

            if (!self.SymbolsByName.TryGetValue(symbol.Name, out var list))
            {
                list = new List<ISymbol>();
                self.SymbolsByName[symbol.Name] = list;
            }

            list.Add(symbol);
        }

        public static ISymbol? LookupSymbol(this ISymbol self, string name)
        {
            if (self.SymbolsByName.TryGetValue(name, out var list))
            {
                return list.Count > 0 ? list[0] : null;
            }

            return null;
        }

        public static List<ISymbol> LookupAllSymbols(this ISymbol self, string name)
        {
            if (self.SymbolsByName.TryGetValue(name, out var list))
            {
                return list;
            }

            return new List<ISymbol>();
        }
    }
}
