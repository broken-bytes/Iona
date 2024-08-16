using Lexer.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexer.Processors
{
    public class NumberProcessor : IProcessor
    {

        public NumberProcessor() {}

        private Token? ProcessInteger(string source)
        {
            var number = new StringBuilder();

            foreach (char c in source)
            {
                if (char.IsDigit(c))
                {
                    number.Append(c);
                }
                else if (char.IsLetter(c))
                {
                    // TODO: Handle error
                    break;
                }
                else if (c == '.')
                {
                    // Potential float, let `process_float` handle it
                    return null;
                }
                else if (Utils.IsOperator(c))
                {
                    break;
                }
            }

            if (number.Length == 0)
            {
                return null;
            }

            return Utils.MakeToken(TokenType.Integer, number.ToString());
        }

        private Token? ProcessFloat(string source)
        {
            bool hasDot = false;
            var number = new StringBuilder();

            foreach (char c in source)
            {
                if (char.IsDigit(c))
                {
                    number.Append(c);
                }
                else if (char.IsLetter(c))
                {
                    // TODO: Handle error
                    break;
                }
                else if (c == '.')
                {
                    if (hasDot)
                    {
                        // TODO: Handle error
                        break;
                    }
                    hasDot = true;
                    number.Append(c);
                }
                else if (Utils.IsOperator(c))
                {
                    break;
                }
            }

            if (number.Length == 0)
            {
                return null;
            }

            return Utils.MakeToken(TokenType.Float, number.ToString());
        }

        private Token? ProcessHex(string source)
        {
            if (source.Length < 3 || !source.StartsWith("0x"))
            {
                return null;
            }

            var number = new StringBuilder();

            foreach (char c in source.Substring(2)) // Skip the "0x" prefix
            {
                if (Uri.IsHexDigit(c)) // Use Uri.IsHexDigit for hex digits
                {
                    number.Append(c);
                }
                else if (char.IsLetter(c))
                {
                    // TODO: Handle error
                    break;
                }
                else if (Utils.IsOperator(c))
                {
                    break;
                }
            }

            if (number.Length == 0)
            {
                return null;
            }

            return Utils.MakeToken(TokenType.Integer, number.ToString());
        }

        private Token? ProcessBinary(string source)
        {
            if (source.Length < 3 || !source.StartsWith("0b"))
            {
                return null;
            }

            var number = new StringBuilder();

            foreach (char c in source.Substring(2)) // Skip the "0b" prefix
            {
                if (c == '0' || c == '1')
                {
                    number.Append(c);
                }
                else if (char.IsLetter(c))
                {
                    // TODO: Handle error
                    break;
                }
                else if (Utils.IsOperator(c))
                {
                    break;
                }
            }

            if (number.Length == 0)
            {
                return null;
            }

            return Utils.MakeToken(TokenType.Integer, number.ToString());
        }

        public Token? Process(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return null;
            }

            if (!char.IsDigit(source[0]))
            {
                return null;
            }

            return ProcessInteger(source)
                ?? ProcessFloat(source)
                ?? ProcessHex(source)
                ?? ProcessBinary(source);
        }
    }
}
