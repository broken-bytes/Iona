namespace Symbols.Symbols
{
    public class PropertySymbol : ISymbol
    {
        public string Name { get; set; }
        public string ILName { get; set; }
        public TypeSymbol Type { get; set; }
        public List<ISymbol> Symbols { get; set; }
        public SymbolKind Kind { get; set; } = SymbolKind.Property;
        public ISymbol? Parent { get; set; }

        public PropertySymbol(string name, TypeSymbol type, bool isIl = false)
        {
            // Since C# usually uses PascalCase for function names,
            // we need convert back and forth between PascalCase and camelCase for Iona
            if (isIl)
            {
                // Lowercase the first letter of the name
                this.Name = name[0].ToString().ToLower() + name.Substring(1);
                Name = name;
                ILName = name;
            }
            else
            {
                Name = name;
                ILName = name[0].ToString().ToUpper() + name.Substring(1);
            }

            Type = type;
            Symbols = new List<ISymbol>();
        }
    }
}
