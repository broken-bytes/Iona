﻿using AST.Nodes;
using Symbols;
using Symbols.Symbols;

namespace Typeck
{
    public interface ITypeck
    {
        public SymbolTable BuildSymbolTable(INode node);
        public void TypeCheck(INode node, SymbolTable table);
        public SymbolTable MergeTables(List<SymbolTable> tables);
    }
}