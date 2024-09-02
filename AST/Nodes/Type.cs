using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class Type
    {
        public enum Kind
        {
            Class,
            Contract,
            Enum,
            Function,
            Struct,
            Unknown
        }

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
