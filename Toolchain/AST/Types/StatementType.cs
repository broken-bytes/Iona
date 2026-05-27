//|--- StatementType.cs ----------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace AST.Types
{
    public enum StatementType
    {
        Import,
        ClassDeclaration,
        ContractDeclaration,
        EnumDeclaration,
        FunctionDeclaration,
        InitDeclaration,
        ModuleDeclaration,
        OperatorDeclaration,
        PropertyDeclaration,
        RecordDeclaration,
        ReturnStatement,
        StructDeclaration,
        VariableDeclaration,
        VariableAssignment,
        Return,
        Guard,
        If,
        While,
        For,
        Break,
        Continue,
    }
}
