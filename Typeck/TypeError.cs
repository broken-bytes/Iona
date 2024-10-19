using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Typeck
{
    internal class TypeError : Exception
    {
        public TypeError(string message) : base(message)
        {
        }
    }
}
