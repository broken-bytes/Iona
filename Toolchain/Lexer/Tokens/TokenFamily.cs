//|--- TokenFamily.cs ------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace Lexer.Tokens
{
    public enum TokenFamily
    {
        Keyword,
        Literal,
        Operator,
        Special,
        Grouping,
        Identifier,
        Error,
        Unknown,
    }
}
