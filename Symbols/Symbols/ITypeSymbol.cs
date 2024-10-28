namespace Symbols.Symbols
{
    public interface ITypeSymbol : ISymbol
    {
        public bool IsArray { get; }
        public bool IsConcrete { get; }
        public bool IsGeneric { get; }
    }
}
