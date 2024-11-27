namespace Symbols.Symbols
{
    public class InitSymbol : ISymbol
    {
        public string Name { get; set; }
        public TypeSymbol ReturnType { get; set; }
        public List<ISymbol> Symbols { get; set; }
        public SymbolKind Kind { get; set; } = SymbolKind.Init;
        public ISymbol? Parent { get; set; }

        public InitSymbol()
        {
            Name = "init";
            ReturnType = new TypeSymbol("", TypeKind.Unknown);
            Symbols = new List<ISymbol>();
        }
    }
}
