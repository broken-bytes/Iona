namespace Typeck.Symbols
{
    public class ModuleSymbol : ISymbol
    {
        public string Name { get; set; }
        public List<ISymbol> Symbols { get; set; }
        public SymbolKind Kind { get; set; } = SymbolKind.Module;
        public ISymbol? Parent { get; set; }

        public ModuleSymbol(string name)
        {
            Name = name;
            Symbols = new List<ISymbol>();
            Parent = null;
        }
    }
}
