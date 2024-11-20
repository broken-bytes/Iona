namespace Symbols.Symbols
{
    public class ModuleSymbol : ISymbol
    {
        public string Name { get; set; }
        public string Assembly { get; set; }
        public List<ISymbol> Symbols { get; set; }
        public SymbolKind Kind { get; set; } = SymbolKind.Module;
        public ISymbol? Parent { get; set; }

        public ModuleSymbol(string name, string assembly, ISymbol? parent = null)
        {
            Name = name;
            Assembly = assembly;
            Symbols = new List<ISymbol>();
            Parent = parent;
        }
    }
}
