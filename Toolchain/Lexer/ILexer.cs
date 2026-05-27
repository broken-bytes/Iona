//|--- ILexer.cs -----------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using Lexer.Tokens;

namespace Lexer
{
    public interface ILexer
    {
        public TokenStream Tokenize(string code, string fileName);
    }
}
