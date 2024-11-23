namespace Symbols.Symbols
{
    public class GenericParameterSymbol(string name) : ISymbol
    {
        public string Name { get; set; } = name;
        public List<ISymbol> Symbols { get; set; } = [];
        public SymbolKind Kind { get; set; } = SymbolKind.GenericParameter;
        public ISymbol? Parent { get; set; }

        // TODO: Add constraints
    }
}