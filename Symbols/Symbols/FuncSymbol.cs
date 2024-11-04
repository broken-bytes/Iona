namespace Symbols.Symbols
{
    public class FuncSymbol : ISymbol
    {
        public string Name { get; set; }
        public string ILName { get; set; }
        public TypeSymbol ReturnType { get; set; }
        public List<ISymbol> Symbols { get; set; }
        public SymbolKind Kind { get; set; } = SymbolKind.Function;
        public ISymbol? Parent { get; set; }

        public FuncSymbol(string name, bool isIl = false)
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

            ReturnType = new TypeSymbol("", TypeKind.Unknown);
            Symbols = new List<ISymbol>();
        }
    }
}
