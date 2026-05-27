//|--- Typeck.cs -----------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using System.Reflection;
using AST.Nodes;
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

        internal Typeck(
            DeclPass declPass,
            ImplPass implPass,
            AssemblyResolver assemblyResolver
        )
        {
            _declPass = declPass;
            _implPass = implPass;
            _assemblyResolver = assemblyResolver;
        }

        public void DoSemanticAnalysis(List<FileNode> files, string assembly, SymbolTable table)
        {
            _declPass.Run(files, table, assembly);
            _implPass.Run(files, table, assembly);
        }

        public void AddImportedAssemblySymbols(SymbolTable table, List<string> assemblies)
        {
            // Register builtin types programmatically (Kotlin-style: no wrapper DLL needed)
            BuiltinTypeRegistrar.RegisterBuiltins(table);

            List<Assembly> loadedAssemblies = [];

            foreach (var path in assemblies)
            {
                Assembly? assembly = null;
                try
                {
                    assembly = System.IO.File.Exists(path)
                        ? Assembly.LoadFrom(path)
                        : Assembly.Load(path);
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
