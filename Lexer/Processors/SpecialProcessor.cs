using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lexer.Processors;
using Lexer.Tokens;

namespace Lexer.Processors
{
    public class SpecialProcessor : IProcessor
    {
        public Token? Process(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return null;
            }

            if(source.First() == '\n')
            {
                return Utils.MakeToken(TokenType.Linebreak, "\n");
            }

            if(source.StartsWith(Special.HardUnwrap.AsString()))
            {
                return Utils.MakeToken(TokenType.HardUnwrap, Special.HardUnwrap.AsString());
            }

            if(source.StartsWith(Special.SoftUnwrap.AsString()))
            {
                return Utils.MakeToken(TokenType.SoftUnwrap, Special.SoftUnwrap.AsString());
            }

            if(source.StartsWith(Special.Colon.AsString()))
            {
                return Utils.MakeToken(TokenType.Colon, Special.Colon.AsString());
            }

            return null;
        }
    }
}
