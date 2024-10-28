namespace Symbols.Symbols
{
    public class GenericTypeSymbol : ITypeSymbol
    {
        public string FullyQualifiedName => GetFullyQualifiedName(this);
        public string Name { get; set; }
        public bool IsArray => true;
        public bool IsConcrete => true;
        public bool IsGeneric => false;
        public List<ITypeSymbol> TypeArguments { get; set; }
        public TypeKind TypeKind { get; }
        public List<ISymbol> Symbols { get; set; }
        public SymbolKind Kind { get; set; } = SymbolKind.Type;
        public ISymbol? Parent { get; set; }

        public GenericTypeSymbol(string name, TypeKind kind)
        {
            Name = name;
            TypeKind = kind;
            TypeArguments = new List<ITypeSymbol>();
            Symbols = new List<ISymbol>();
        }

        public ISymbol? FindMember(string name) =>
            Symbols.First(child => child is BlockSymbol).Symbols.Find(symbol => symbol.Name == name);

        private static string GetFullyQualifiedName(ISymbol symbol)
        {
            var name = symbol.Name;
            var parent = symbol.Parent;

            while (parent != null)
            {
                name = $"{parent.Name}.{name}";
                parent = parent.Parent;
            }

            return name;
        }
    }
}
