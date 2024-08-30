using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AST.Types
{
    public enum NodeKind
    {
        Array,
        Class,
        Contract,
        Enum,
        File,
        Module,
        Struct,
        Function,
        Variable,
        UnknownKind,
        Primitive,
    }
}
