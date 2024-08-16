using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AST.Types
{
    public enum OperatorType
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        Equal,
        NotEqual,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        And,
        Or,
        Xor,
        Not,
        Negate,
        BitwiseAnd,
        BitwiseOr,
        BitwiseXor,
        BitwiseNot,
        BitwiseLeftShift,
        BitwiseRightShift,
        Assign,
        AddAssign,
        SubtractAssign,
        MultiplyAssign,
        DivideAssign,
        ModuloAssign,
        AndAssign,
        OrAssign,
        XorAssign,
        BitwiseAndAssign,
        BitwiseOrAssign,
        BitwiseXorAssign,
        BitwiseLeftShiftAssign,
        BitwiseRightShiftAssign,
        Increment,
        Decrement,
        InvalidOperator,
        Noop,
    }
}
