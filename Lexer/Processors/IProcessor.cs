using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lexer.Tokens;

namespace Lexer.Processors
{
    public interface IProcessor
    {
        Token? Process(string source);
    }
}
