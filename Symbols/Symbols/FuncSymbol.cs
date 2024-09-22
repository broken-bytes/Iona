namespace Symbols.Symbols
{
    public class FuncSymbol : ISymbol
    {
        public string Name { get; set; }
        public TypeSymbol ReturnType { get; set; }
        public List<ParameterSymbol> Parameters { get; set; }
        public List<ISymbol> Symbols { get; set; }
        public SymbolKind Kind { get; set; } = SymbolKind.Function;
        public ISymbol? Parent { get; set; }

        public FuncSymbol(string name)
        {
            Name = name;
            ReturnType = new TypeSymbol("", TypeKind.Unknown);
            Parameters = new List<ParameterSymbol>();
            Symbols = new List<ISymbol>();
        }
    }
}
