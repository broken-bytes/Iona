//|--- OperatorType.cs -----------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

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
