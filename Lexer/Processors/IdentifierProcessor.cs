using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            
            return Utils.MakeToken(TokenType.Identifier, tokenValue);
        }
    }
}
