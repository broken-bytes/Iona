using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AST.Types
{
    public enum UnaryOperation
    {
        Negation,
        Increment,
        Decrement,
        Not,
        BitwiseAnd,
        BitwiseNot,
        BitwiseOr,
        Noop,
    }
}
