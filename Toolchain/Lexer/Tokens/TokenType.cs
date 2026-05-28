//|--- TokenType.cs --------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace Lexer.Tokens
{
    public enum TokenType
    {
        // Special Tokens
        Error,
        EndOfFile,
        Invalid,

        // Keywords
        // Types
        Class,
        Contract,
        Enum,
        Module,
        Record,
        Self,
        Struct,
        Super,
        Override,

        // Access Modifiers
        Fileprivate,
        Internal,
        Open,
        Private,
        Public,
        Static,

        // Variables & Constants
        Let,
        Var,

        // Control Flow
        Async,
        Await,
        Break,
        Catch,
        Continue,
        Defer,
        Do,
        Else,
        Finally,
        For,
        Fn, // Func in C++
        Guard,
        If,
        Use, // Import in C++
        In,
        Init,
        Mutating,
        Of,
        Op,
        Return,
        Throw,
        Throws,
        Try,
        Until,
        When,
        While,
        With,
        Yield,

        // Operators
        // Arithmetic
        Assign,
        BitAndAssign,
        BitOrAssign,
        BitXorAssign,
        BitLShiftAssign,
        BitRShiftAssign,
        Divide,
        DivideAssign,
        Minus,
        MinusAssign,
        Modulo,
        ModAssign,
        Multiply,
        MultiplyAssign,
        Plus,
        PlusAssign,
        Increment,
        Decrement,

        // Bitwise
        BitAnd,
        BitInverse,
        BitLShift,
        BitNegate,
        BitOr,
        BitRShift,
        Xor,

        // Logical
        And,
        Not,
        Or,

        // Comparison
        Equal,
        ArrowRight,
        GreaterEqual,
        ArrowLeft,
        LessEqual,
        NotEqual,

        // Range
        Range, // ...

        // Other Operators
        Annotation,
        Arrow,
        As,
        Binding,
        Colon,
        Comma,
        Dot,
        ForceOptional,
        NameOverride,
        Optional,
        Pipe,
        HardUnwrap,
        Scope,
        SoftUnwrap,

        // Literals
        Boolean,
        Double,
        False,
        Float,
        Integer,
        NullLiteral,
        String,
        True,

        // Brackets
        BracketLeft,
        BracketRight,
        CurlyLeft,
        CurlyRight,
        ParenLeft,
        ParenRight,

        // Identifiers and Comments
        Captured,
        Identifier,
        Linebreak,
        MultiLineComment,

        // Parser-synthesized markers for prefix unary operators (re-tagged from Minus/Not)
        UnaryMinus,
        UnaryNot,
    }
}

