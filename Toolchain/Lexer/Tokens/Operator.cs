//|--- Operator.cs ---------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

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
        Scope,
        ShiftLeft,
        ShiftRight,
        ShiftLeftAssign,
        ShiftRightAssign,
        Ternary,
        Arrow,
        Dot,
    }
}
