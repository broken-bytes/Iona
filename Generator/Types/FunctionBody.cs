using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generator.Types
{
    internal class FunctionBody
    {
        private readonly ILProcessor _processor;

        public ILProcessor Processor => _processor!;

        internal FunctionBody()
        {
            _processor = new ILProcessor();
        }
    }
}
