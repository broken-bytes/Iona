//|--- UnaryOperation.cs ---------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

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
