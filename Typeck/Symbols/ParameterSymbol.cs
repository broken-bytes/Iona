namespace Typeck.Symbols
{
    public class ParameterSymbol
    {
        public string Name { get; set; }
        public TypeSymbol Type { get; set; }

        public ParameterSymbol(string name, TypeSymbol type)
        {
            Name = name;
            Type = type;
        }
    }
}
