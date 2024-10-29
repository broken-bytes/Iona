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
        private IErrorCollector _errorCollector;

        internal Generator(IErrorCollector errorCollector)
        {
            _errorCollector = errorCollector;
        }

        public Assembly CreateAssembly(string name, SymbolTable table)
        {
            return new Assembly(name, table);
        }
    }
}
