using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    public interface ICompiler
    {
        public void Compile(string source, string filename);
    }
}
