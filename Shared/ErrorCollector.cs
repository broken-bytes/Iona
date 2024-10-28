using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    internal class ErrorCollector : IErrorCollector
    {
        public List<CompilerError> Errors { get; private set; }
        public bool HasErrors => Errors.Any();

        internal ErrorCollector()
        {
            Errors = new List<CompilerError>();
        }

        public void Collect(CompilerError error)
        {
            Errors.Add(error);
        }
    }
}
