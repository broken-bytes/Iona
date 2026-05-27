//|--- IdentifierProcessor.cs ----------------------------------|
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
    public class IdentifierProcessor : IProcessor
    {
        public Token? Process(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return null;
            }

            char firstChar = source[0];
            if (!char.IsLetter(firstChar) && firstChar != '_')
            {
                return null;
            }

            int i = 1;
            while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] == '_'))
            {
                i++;
            }

            string tokenValue = source.Substring(0, i);

            // `true`/`false` are boolean literals, not identifiers (word-boundary-safe here,
            // so `trueValue` stays an identifier).
            if (tokenValue is "true" or "false")
            {
                return Utils.MakeToken(TokenType.Boolean, tokenValue);
            }

            return Utils.MakeToken(TokenType.Identifier, tokenValue);
        }
    }
}
