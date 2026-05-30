//|--- PunctuationProcessor.cs ---------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using Lexer.Tokens;

namespace Lexer.Processors
{
    public class PunctuationProcessor : IProcessor
    {
        public Token? Process(string source)
        {
            if (string.IsNullOrEmpty(source) || char.IsLetterOrDigit(source[0]))
            {
                return null;
            }

            // Check if the first character is a comma
            if (Utils.CheckMatchingSequence(source, Special.Comma.AsString()))
            {
                return Utils.MakeToken(TokenType.Comma, Special.Comma.AsString());
            }

            // `#` is the declaration-directive marker (`#over<T>`, `#packed`, `#align(8)`).
            if (source[0] == '#')
            {
                return Utils.MakeToken(TokenType.Hash, "#");
            }

            return null;
        }
    }
}
