using AST.Nodes;
using AST.Visitors;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Shared;
using Symbols;
using System;

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
