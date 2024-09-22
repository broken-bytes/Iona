namespace Symbols.Symbols
{
    public class BlockSymbol : ISymbol
    {
        public string Name { get; set; }
        public List<ISymbol> Symbols { get; set; }
        public SymbolKind Kind { get; set; } = SymbolKind.Scope;
        public ISymbol? Parent { get; set; }

        public BlockSymbol()
        {
            Name = "";
            Symbols = new List<ISymbol>();
        }
    }
}
