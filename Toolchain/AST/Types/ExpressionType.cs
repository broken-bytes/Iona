//|--- ExpressionType.cs ---------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace AST.Types
{
    public enum ExpressionType
    {
        EnumCaseAcces,
        Literal,
        Identifier,
        FunctionCall,
        BinaryOperation,
        ComparisonOperation,
        UnaryOperation,
        TypeCast,
        TypeOf,
        SizeOf,
        ArrayAccess,
        MemberAccess,
        ObjectLiteral,
        PointerAccess,
        PointerDereference,
        ForceUnwrap,
        PropAccess,
        AddressOf,
        ScopeResolution,
        Self,
        New,
        Delete,
        Lambda,
        Tuple,
        Array,
        Struct,
        Union,
        Enum,
        If,
        While,
        For,
        Switch,
        Return,
        Break,
        Continue,
        Label,
        Block,
        Parentheses,
        Await,
        Error,
        Noop,
    }
}
