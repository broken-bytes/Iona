using System.Reflection;
using AST.Nodes;
using AST.Types;
using Symbols;
using Typeck.Passes;
using Typeck.Passes.Impl;

namespace Typeck
{
    internal class Typeck : ITypeck
    {
        private readonly DeclPass _declPass;
        private readonly ImplPass _implPass;
        private readonly AssemblyResolver _assemblyResolver;
        private readonly SymbolTableConstructor _tableConstructor;
        private readonly TopLevelScopeResolver _topLevelScopeResolver;
        private readonly TypeResolver _typeResolver;
        private readonly ExpressionResolver _expressionResolver;
        private readonly MutabilityResolver _mutabilityResolver;

        internal Typeck(
            DeclPass declPass,
            ImplPass implPass,
            AssemblyResolver assemblyResolver,
            SymbolTableConstructor tableConstructor,
            TopLevelScopeResolver topLevelScopeResolver,
            TypeResolver typeResolver,
            ExpressionResolver expressionResolver,
            MutabilityResolver mutabilityResolver
        )
        {
            _declPass = declPass;
            _implPass = implPass;
            _assemblyResolver = assemblyResolver;
            _tableConstructor = tableConstructor;
            _topLevelScopeResolver = topLevelScopeResolver;
            _typeResolver = typeResolver;
            _expressionResolver = expressionResolver;
            _mutabilityResolver = mutabilityResolver;
        }

        public void DoSemanticAnalysis(INode node, string assembly, SymbolTable table)
        {
            // The root node shall be a file node, but we strip it and only add the module
            if (node is not FileNode fileNode)
            {
                // Panic
                return;
            }
            
            _declPass.Run(fileNode, table, assembly);
            _implPass.Run(fileNode, table, assembly);
        }

        public void AddImportedAssemblySymbols(SymbolTable table, List<string> assemblies)
        {
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
            
            _assemblyResolver.AddAssembliesToSymbolTable(loadedAssemblies, table);
        }
    }
}
