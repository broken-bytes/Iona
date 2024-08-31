using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexer.Tokens
{
    public enum TokenFamily
    {
        Keyword,
        Literal,
        Operator,
        Special,
        Grouping,
        Identifier,
        Error,
        Unknown,
    }
}
