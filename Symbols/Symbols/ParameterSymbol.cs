namespace Symbols.Symbols
{
    public class ParameterSymbol : ISymbol
    {
        public string Name { get; set; }
        public TypeSymbol Type { get; set; }
        public SymbolKind Kind { get; set; } = SymbolKind.Parameter;
        public ISymbol? Parent { get; set; }
        public List<ISymbol> Symbols { get; set; } = new List<ISymbol>();

        public ParameterSymbol(string name, TypeSymbol type)
        {
            Name = name;
            Type = type;
        }
    }
}
