namespace Symbols.Symbols
{
    public class ParameterSymbol : ISymbol
    {
        public string Name { get; set; }
        public TypeSymbol Type { get; set; }
        public SymbolKind Kind { get; set; } = SymbolKind.Parameter;
        public bool IsGenericParameter { get; set; } = false;
        public ISymbol? Parent { get; set; }
        public List<ISymbol> Symbols { get; set; } = new List<ISymbol>();

        public ParameterSymbol(string name, TypeSymbol type, ISymbol? parent)
        {
            Name = name;
            Type = type;
            Parent = parent;
        }
        
        public ParameterSymbol(string name, bool isGenericParameter, ISymbol? parent)
        {
            Name = name;
            IsGenericParameter = isGenericParameter;
            Type = new TypeSymbol(name, TypeKind.Generic);
            Parent = parent;
        }
    }
}
