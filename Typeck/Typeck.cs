using System.Reflection;
using AST.Nodes;
using AST.Types;
using Symbols;

namespace Typeck
{
    internal class Typeck : ITypeck
    {
        private readonly AssemblyResolver _assemblyResolver;
        private readonly SymbolTableConstructor _tableConstructor;
        private readonly TopLevelScopeResolver _topLevelScopeResolver;
        private readonly TypeResolver _typeResolver;
        private readonly ExpressionResolver _expressionResolver;
        private readonly MutabilityResolver _mutabilityResolver;

        internal Typeck(
            AssemblyResolver assemblyResolver,
            SymbolTableConstructor tableConstructor,
            TopLevelScopeResolver topLevelScopeResolver,
            TypeResolver typeResolver,
            ExpressionResolver expressionResolver,
            MutabilityResolver mutabilityResolver
        )
        {
            _assemblyResolver = assemblyResolver;
            _tableConstructor = tableConstructor;
            _topLevelScopeResolver = topLevelScopeResolver;
            _typeResolver = typeResolver;
            _expressionResolver = expressionResolver;
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
                _expressionResolver.CheckScopes(fileNode, table);
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

            List<Assembly> loadedAssemblies = [];
            
            foreach (var path in assemblies)
            {
                Assembly? assembly = null;
                try
                {
                    assembly = Assembly.Load(path);
                    loadedAssemblies.Add(assembly);
                }
                catch
                {
                    continue;
                }
            }
            
            _assemblyResolver.AddAssembliesToSymbolTable(loadedAssemblies, mergedTable);
            
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
