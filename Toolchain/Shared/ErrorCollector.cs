//|--- ErrorCollector.cs ---------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

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
