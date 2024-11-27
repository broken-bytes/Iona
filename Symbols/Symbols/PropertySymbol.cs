using AST.Types;

namespace Symbols.Symbols
{
    public class PropertySymbol : ISymbol
    {
        public string Name { get; set; }
        public string CsharpName { get; set; }
        public TypeSymbol Type { get; set; }
        public List<ISymbol> Symbols { get; set; }
        public SymbolKind Kind { get; set; } = SymbolKind.Property;
        public ISymbol? Parent { get; set; }
        public AccessLevel GetterAccessLevel { get; set; }
        public AccessLevel SetterAccessLevel { get; set; }
        public static bool IsStatic { get; set; }

        public PropertySymbol(
            string name, 
            string csharpName,
            TypeSymbol type, 
            bool isStatic,
            bool isComputed = true,
            AccessLevel getterAccessLevel = AccessLevel.Private,
            AccessLevel setterAccessLevel = AccessLevel.Private
            )
        {
            Name = name;
            CsharpName = csharpName;
            Type = type;
            Symbols = new List<ISymbol>();
            GetterAccessLevel = getterAccessLevel;
            SetterAccessLevel = setterAccessLevel;
            IsStatic = isStatic;
        }
    }
}
