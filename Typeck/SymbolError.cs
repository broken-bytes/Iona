﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Typeck
{
    internal class SymbolError : Exception
    {
        public SymbolError(string message) : base(message)
        {
        }
    }
}
