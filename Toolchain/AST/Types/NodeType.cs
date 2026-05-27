//|--- NodeType.cs ---------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace AST.Types
{
    public enum NodeType
    {
        Allocation,
        ArrayAccess,
        ArrayLiteral,
        Assignment,
        Contract,
        Deallocation,
        Declaration,
        EnumCaseAccess,
        Expression,
        GenericType,
        GenericArgument,
        Statement,
        NominalType,
        Import,
        Init,
        Func,
        FuncCall,
        Literal,
        MemberAccess,
        Identifier,
        ObjectLiteral,
        Operator,
        Parameter,
        ParameterAccess,
        PropAccess,
        ScopeResolution,
        Self,
        GuardStatement,
        IfStatement,
        ElseStatement,
        ForceUnwrap,
        ForLoop,
        WhileLoop,
        BreakStatement,
        ContinueStatement,
        ReturnStatement,
        TryBlock,
        CatchBlock,
        File,
        Module,
        Namespace,
        Annotation,
        Program,
        CodeBlock,
        Error,
        TypeReference,
        VarAccess
    }
}
