namespace AST.Types
{
    public interface IType
    {
        public string Name { get; }
        public string Module { get; set; }
        public Kind TypeKind { get; set; }
    }
}
