namespace Symbols.Symbols
{
    public enum TypeKind
    {
        Class,
        Contract,
        Enum,
        Generic,
        Struct,
        /// Primitive is only used inside of the Standard Library to represent primitive types (int, bool, etc.) from the CLR.
        Primitive,
        Unknown
    }
}
