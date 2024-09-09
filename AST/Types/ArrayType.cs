namespace AST.Types
{
    public class ArrayType : IType
    {
        public string Name => "Array";
        public string Module { get; set; }
        public Kind TypeKind { get; set; }
        public IType ElementType { get; set; }

        public ArrayType(IType element)
        {
            ElementType = element;
            Module = "";
            TypeKind = Kind.Unknown;
        }
    }
}
