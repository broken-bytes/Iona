namespace Symbols.Symbols
{
    public class VariableSymbol : ISymbol
    {
        public string Name { get; set; }
        public TypeSymbol Type { get; set; }
        public List<ISymbol> Symbols { get; set; }
        public SymbolKind Kind { get; set; } = SymbolKind.Variable;
        public ISymbol? Parent { get; set; }

        public VariableSymbol(string name, TypeSymbol type)
        {
            Name = name;
            Type = type;
            Symbols = new List<ISymbol>();
        }
    }
}
