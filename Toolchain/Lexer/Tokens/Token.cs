//|--- Token.cs ------------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace Lexer.Tokens
{
    public struct Token
    {
        public TokenFamily Family;
        public TokenType Type;
        public string Value;
        public string File;
        public int Line;
        public int ColumnStart;
        public int ColumnEnd;
        public string Error;
    }
}
