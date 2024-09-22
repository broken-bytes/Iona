using AST.Nodes;
using AST.Visitors;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Symbols;
using System;

namespace Generator
{
    public class Generator : IGenerator
    {
        public Assembly CreateAssembly(string name, SymbolTable table)
        {
            return new Assembly(name, table);
        }
    }
}
