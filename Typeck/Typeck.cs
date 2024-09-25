using AST.Nodes;
using AST.Types;
using Symbols;

namespace Typeck
{
    internal class Typeck : ITypeck
    {
        private readonly SymbolTableConstructor _tableConstructor;
        private readonly ScopeChecker _scopeChecker;
        private readonly TypeChecker _typeChecker;

        internal Typeck(
            SymbolTableConstructor tableConstructor, 
            ScopeChecker scopeChecker,
            TypeChecker typeChecker
        )
        {
            _tableConstructor = tableConstructor;
            _scopeChecker = scopeChecker;
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

#if !IONA_BOOTSTRAP
            // Add the builtins module to the imports of the file node
            fileNode.Children.Insert(0, new ImportNode("Builtins", fileNode));
#endif

            return table;
        }

        public void ScopeCheck(INode node, SymbolTable table)
        {
            if (node is FileNode fileNode)
            {
                _scopeChecker.CheckScopes(fileNode, table);
            }
        }

        public void TypeCheck(INode node, SymbolTable table)
        {
            if (node is FileNode fileNode)
            {
                _typeChecker.TypeCheckAST(fileNode, table);
            }
        }

        public SymbolTable MergeTables(List<SymbolTable> tables)
        {
            var mergedTable = new SymbolTable();

            foreach (var table in tables)
            {
                foreach (var module in table.Modules)
                {
                    mergedTable.Modules.Add(module);
                }
            }

            return mergedTable;
        }
    }
}
