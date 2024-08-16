using Lexer.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexer.Processors
{
    public class LiteralProcessor : IProcessor
    {
        private IProcessor numberProcessor;

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
                return Utils.MakeToken(TokenType.True, Keyword.True.AsString());
            }
            else if (Utils.CheckMatchingSequence(source, Keyword.False.AsString()))
            {
                return Utils.MakeToken(TokenType.False, Keyword.False.AsString());
            }

            return null;
        }

        public Token? ProcessStringLiteral(string source)
        {
            if (!source.StartsWith('"'))
            {
                return null;
            }

            // Find the closing quote
            int closingQuoteIndex = source.IndexOf('"', 1); // Start search from index 1 to skip the opening quote

            if (closingQuoteIndex == -1)
            {
                // Missing closing quote
                return Utils.MakeToken(TokenType.Error, "Invalid string literal. Missing closing quote.");
            }
            else if (closingQuoteIndex == 1)
            {
                // Empty string
                return Utils.MakeToken(TokenType.String, "");
            }
            else
            {
                string literalValue = source.Substring(1, closingQuoteIndex - 1); // Extract the literal value
                return Utils.MakeToken(TokenType.String, literalValue);
            }
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
