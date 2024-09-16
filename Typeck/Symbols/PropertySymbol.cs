namespace Typeck.Symbols
{
    public class PropertySymbol : ISymbol
    {
        public string Name { get; set; }
        public TypeSymbol Type { get; }
        public List<ISymbol> Symbols { get; set; }
        public SymbolKind Kind { get; set; } = SymbolKind.Property;
        public ISymbol? Parent { get; set; }

        public PropertySymbol(string name, TypeSymbol type)
        {
            Name = name;
            Type = type;
            Symbols = new List<ISymbol>();
        }
    }
}
