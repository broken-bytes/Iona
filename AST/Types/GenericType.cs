using AST.Visitors;

namespace AST.Types
{
    public class GenericType : IType
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public Kind TypeKind { get; set; }
        public List<IType> GenericArguments { get; set; } = new List<IType>();

        public GenericType(string name)
        {
            Name = name;
            Module = "";
            TypeKind = Kind.Unknown;
        }

        public GenericType(string name, string module, List<IType> genericArguments, Kind kind = Kind.Unknown)
        {
            Name = name;
            Module = module;
            GenericArguments = genericArguments;
            TypeKind = kind;
        }
    }
}
