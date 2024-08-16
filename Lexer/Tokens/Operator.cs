using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexer.Tokens
{
    public enum Operator
    {
        Add,
        Sub,
        Mul,
        Div,
        Mod,
        Pow,
        Inc,
        Dec,
        Assign,
        AddAssign,
        SubAssign,
        MulAssign,
        DivAssign,
        ModAssign,
        PowAssign,
        And,
        Or,
        Xor,
        Not,
        AndAssign,
        OrAssign,
        XorAssign,
        NotAssign,
        AndAnd,
        OrOr,
        Equal,
        NotEqual,
        Less,
        Greater,
        LessEqual,
        GreaterEqual,
        ShiftLeft,
        ShiftRight,
        ShiftLeftAssign,
        ShiftRightAssign,
        Ternary,
        Arrow,
        Dot,
    }
}
