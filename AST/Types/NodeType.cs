﻿namespace AST.Types
{
    public enum NodeType
    {
        Allocation,
        ArrayLiteral,
        Assignment,
        Contract,
        Deallocation,
        Declaration,
        Expression,
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
        IfStatement,
        ElseStatement,
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
    }
}
