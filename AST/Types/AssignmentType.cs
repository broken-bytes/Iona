using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AST.Types
{
    public enum AssignmentType
    {
        Assign,
        AddAssign,
        SubAssign,
        MulAssign,
        DivAssign,
        ModAssign,
        AndAssign,
        OrAssign,
        XorAssign,
        NotAssign,
        ShlAssign,
        ShrAssign,
        Noop,
    }
}
