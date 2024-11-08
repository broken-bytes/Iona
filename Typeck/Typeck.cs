using System.Reflection;
using AST.Nodes;
using AST.Types;
using Symbols;

namespace Typeck
{
    internal class Typeck : ITypeck
    {
        private readonly SymbolTableConstructor _tableConstructor;
        private readonly TopLevelScopeResolver _topLevelScopeResolver;
        private readonly TypeResolver _typeResolver;
        private readonly ExpressionScopeResolver _expressionScopeResolver;
        private readonly MutabilityResolver _mutabilityResolver;

        internal Typeck(
            SymbolTableConstructor tableConstructor,
            TopLevelScopeResolver topLevelScopeResolver,
            TypeResolver typeResolver,
            ExpressionScopeResolver expressionScopeResolver,
            MutabilityResolver mutabilityResolver
        )
        {
            _tableConstructor = tableConstructor;
            _topLevelScopeResolver = topLevelScopeResolver;
            _typeResolver = typeResolver;
            _expressionScopeResolver = expressionScopeResolver;
            _mutabilityResolver = mutabilityResolver;
        }

        public SymbolTable BuildSymbolTable(INode node, string assembly)
        {
            SymbolTable table;

            // The root node shall be a file node, but we strip it and only add the module
            if (node is not FileNode fileNode)
            {
                // Panic
                return new SymbolTable();
            }

            _tableConstructor.ConstructSymbolTable(fileNode, out table, assembly);

#if !IONA_BOOTSTRAP
            // Add the builtins module to the imports of the file node
            fileNode.Children.Insert(0, new ImportNode("Iona.Builtins", fileNode));
#endif

            return table;
        }

        public void DoSemanticAnalysis(INode node, SymbolTable table)
        {
            CheckTopLevelScopes(node, table);
            TypeCheck(node, table);
            CheckExpressions(node, table);
        }

        public void CheckTopLevelScopes(INode node, SymbolTable table)
        {
            if (node is FileNode fileNode)
            {
                _topLevelScopeResolver.CheckScopes(fileNode, table);
            }
        }

        public void CheckExpressions(INode node, SymbolTable table)
        {
            if (node is FileNode fileNode)
            {
                _expressionScopeResolver.CheckScopes(fileNode, table);
            }
        }

        public void TypeCheck(INode node, SymbolTable table)
        {
            if (node is FileNode fileNode)
            {
                _typeResolver.TypeCheckAST(fileNode, table);
            }
        }

        public SymbolTable MergeTables(List<SymbolTable> tables, List<string> assemblies)
        {
            var mergedTable = new SymbolTable();
            
            foreach (var path in assemblies)
            {
                Assembly? assembly = null;
                try
                {
                    assembly = Assembly.Load(path);
                    _tableConstructor.ConstructSymbolsForAssembly(assembly);

                }
                catch
                {
                    continue;
                }
            }

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
