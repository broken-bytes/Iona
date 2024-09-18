using AST.Nodes;
using AST.Types;
using Typeck.Symbols;

namespace Typeck
{
    internal class Typeck : ITypeck
    {
        private readonly SymbolTableConstructor _tableConstructor;
        private readonly TypeChecker _typeChecker;

        internal Typeck(SymbolTableConstructor tableConstructor, TypeChecker typeChecker)
        {
            _tableConstructor = tableConstructor;
            _typeChecker = typeChecker;
        }

        public SymbolTable BuildSymbolTable(INode node)
        {
            SymbolTable table;

            // The root node shall be a file node, but we strip it and only add the module
            if (node is not FileNode fileNode)
            {
                // Panic
                return new SymbolTable();
            }

            _tableConstructor.ConstructSymbolTable(fileNode, out table);

            return table;
        }

        public void TypeCheck(INode node, SymbolTable table)
        {
            if (node is FileNode fileNode)
            {
                _typeChecker.TypeCheckAST(ref fileNode, table);
            }
        }

        public SymbolTable MergeTables(List<SymbolTable> tables)
        {
            var mergedTable = new SymbolTable();

            RegisterBuiltins(mergedTable);

            foreach (var table in tables)
            {
                foreach (var module in table.Modules)
                {
                    mergedTable.Modules.Add(module);
                }
            }

            return mergedTable;
        }

        // ---- Helper methods ----
        // ---- Type checking ----
        private void RegisterBuiltins(SymbolTable table)
        {
            // Add the builtins to the global table (Int, Float, String, Bool)
            var intType = new TypeSymbol("Int", TypeKind.Struct);
            var floatType = new TypeSymbol("Float", TypeKind.Struct);
            var stringType = new TypeSymbol("String", TypeKind.Class);
            var boolType = new TypeSymbol("Bool", TypeKind.Struct);

            var builtins = new ModuleSymbol("Builtins");
            ((ISymbol)builtins).AddSymbol(intType);
            ((ISymbol)builtins).AddSymbol(floatType);
            ((ISymbol)builtins).AddSymbol(stringType);
            ((ISymbol)builtins).AddSymbol(boolType);

            table.Modules.Add(builtins);
        }
    }
}
