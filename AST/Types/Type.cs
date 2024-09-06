using AST.Visitors;

namespace AST.Types
{
    public class Type : IType
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public Kind TypeKind { get; set; }

        public Type(string name)
        {
            Name = name;
            Module = "";
            TypeKind = Kind.Unknown;
        }

        public Type(string name, string module, Kind kind)
        {
            Name = name;
            Module = module;
            TypeKind = kind;
        }
    }
}
