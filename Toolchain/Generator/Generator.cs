//|--- Generator.cs --------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Nodes;
using AST.Visitors;
using Shared;
using Symbols;
using System;
using System.Reflection.Emit;
using System.Reflection;

namespace Generator
{
    public class Generator : IGenerator
    {
        private readonly IErrorCollector _errorCollector;
        private readonly IWarningCollector _warningCollector;
        private readonly IFixItCollector _fixItCollector;

        internal Generator
            (IErrorCollector errorCollector,
            IWarningCollector warningCollector,
            IFixItCollector fixItCollector
        )
        {
            _errorCollector = errorCollector;
            _warningCollector = warningCollector;
            _fixItCollector = fixItCollector;
        }

        public Assembly CreateAssembly(string name, SymbolTable table)
        {
            return new Assembly(name, table);
        }
    }
}
