//|--- LiteralProcessor.cs -------------------------------------|
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
    public class LiteralProcessor : IProcessor
    {
        private readonly IProcessor numberProcessor;

        public LiteralProcessor(IProcessor numberProcessor)
        {
            this.numberProcessor = numberProcessor;
        }

        public Token? ProcessNumericLiteral(string source)
        {
            return numberProcessor.Process(source);
        }

        public Token? ProcessBooleanLiteral(string source)
        {
            if (Utils.CheckMatchingSequence(source, Keyword.True.AsString()))
            {
                return Utils.MakeToken(TokenType.Boolean, Keyword.True.AsString());
            }
            else if (Utils.CheckMatchingSequence(source, Keyword.False.AsString()))
            {
                return Utils.MakeToken(TokenType.Boolean, Keyword.False.AsString());
            }

            return null;
        }

        public Token? ProcessStringLiteral(string source)
        {
            if (!source.StartsWith('"'))
            {
                return null;
            }

            // Walk the string body, tracking `${ ... }` interpolation depth, nested string
            // literals inside template expressions, and `\` escapes (so `\$`, `\"`, `\\`
            // don't trip the scanner).
            int end = ScanStringBody(source, 1);
            if (end < 0)
            {
                return Utils.MakeToken(TokenType.Error, "Invalid string literal. Missing closing quote.");
            }

            if (end == 1)
            {
                return Utils.MakeToken(TokenType.String, "\"\"");
            }

            string literalValue = source.Substring(1, end - 1);
            return Utils.MakeToken(TokenType.String, $"\"{literalValue}\"");
        }

        // Returns the index of the closing `"` for a string literal whose body starts at `start`,
        // or -1 if unterminated. Mutually recursive with ScanInterpolation so nested `"..."`
        // inside `${...}` is tolerated.
        private static int ScanStringBody(string s, int start)
        {
            int i = start;
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    i += 2;
                    continue;
                }
                if (c == '"')
                {
                    return i;
                }
                if (c == '$' && i + 1 < s.Length && s[i + 1] == '{')
                {
                    int after = ScanInterpolation(s, i + 2);
                    if (after < 0)
                    {
                        return -1;
                    }
                    i = after;
                    continue;
                }
                i++;
            }
            return -1;
        }

        // Returns the index immediately after the matching `}` for an interpolation that
        // starts after `${`, or -1 if unterminated.
        private static int ScanInterpolation(string s, int start)
        {
            int i = start;
            int depth = 1;
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    i += 2;
                    continue;
                }
                if (c == '"')
                {
                    int innerEnd = ScanStringBody(s, i + 1);
                    if (innerEnd < 0)
                    {
                        return -1;
                    }
                    i = innerEnd + 1;
                    continue;
                }
                if (c == '{')
                {
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i + 1;
                    }
                }
                i++;
            }
            return -1;
        }

        public Token? ProcessNullLiteral(string source)
        {
            if (Utils.CheckMatchingSequence(source, Keyword.Null.AsString()))
            {
                return Utils.MakeToken(TokenType.NullLiteral, Keyword.Null.AsString());
            }

            return null;
        }

        public Token? Process(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return null;
            }

            var token = ProcessNumericLiteral(source);
            if (token != null)
            {
                return token;
            }

            token = ProcessBooleanLiteral(source);
            if (token != null)
            {
                return token;
            }

            token = ProcessStringLiteral(source);
            if (token != null)
            {
                return token;
            }

            token = ProcessNullLiteral(source);
            if (token != null)
            {
                return token;
            }

            return null;
        }
    }
}
