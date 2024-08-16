using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lexer.Tokens;

namespace Lexer.Processors
{
    internal class GroupingProcessor : IProcessor
    {
        public Token? Process(string source)
        {
            // Check if the first character is a punctuation
            if (string.IsNullOrEmpty(source) || char.IsLetterOrDigit(source[0]))
            {
                return null;
            }

            if (source.StartsWith(Grouping.ParenLeft.AsString()))
            {
                return Utils.MakeToken(TokenType.ParenLeft, Grouping.ParenLeft.AsString());
            }

            if (source.StartsWith(Grouping.ParenRight.AsString()))
            {
                return Utils.MakeToken(TokenType.ParenRight, Grouping.ParenRight.AsString());
            }

            if (source.StartsWith(Grouping.BracketLeft.AsString()))
            {
                return Utils.MakeToken(TokenType.BracketLeft, Grouping.BracketLeft.AsString());
            }

            if (source.StartsWith(Grouping.BracketRight.AsString()))
            {
                return Utils.MakeToken(TokenType.BracketRight, Grouping.BracketRight.AsString());
            }

            if (source.StartsWith(Grouping.CurlyLeft.AsString()))
            {
                return Utils.MakeToken(TokenType.CurlyLeft, Grouping.CurlyLeft.AsString());
            }

            if (source.StartsWith(Grouping.CurlyRight.AsString()))
            {
                return Utils.MakeToken(TokenType.CurlyRight, Grouping.CurlyRight.AsString());
            }

            return null;
        }
    }
}
