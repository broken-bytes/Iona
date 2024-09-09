namespace AST.Types
{
    public class ErrorType : IType
    {
        public string Name { get; }
        public string Module { get; set; }
        public Kind TypeKind { get; set; }

        public ErrorType(string name)
        {
            Name = name;
            Module = "";
            TypeKind = Kind.Unknown;
        }
    }
}
