//|--- SymbolTable.cs ------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using Symbols.Symbols;
using AST.Nodes;
using AST.Types;
using System.Xml.Linq;
using AST;
using Shared;

namespace Symbols
{
    public class SymbolTable
    {
        public List<ModuleSymbol> Modules;
        public Dictionary<string, ModuleSymbol> ModulesByName;

        public SymbolTable()
        {
            Modules = new List<ModuleSymbol>();
            ModulesByName = new Dictionary<string, ModuleSymbol>();
        }

        public void AddModule(ModuleSymbol module)
        {
            Modules.Add(module);
            ModulesByName[module.Name] = module;
        }

        public ISymbol? FindBy(INode node)
        {
            // First, get the tree hierarchy of the node
            var hierarchy = node.Hierarchy();

            // First, we need to find the name of the symbol to be found
            var name = GetNodeName(node);

            if (name == null)
            {
                return null;
            }

            // We have a few cases to consider:
            // - A symbol is ALWAYS contained in a block
            // - Blocks are contained in functions, inits, operators, and types
            // Thus we need to check the parent of the node, check if the block contains the symbol
            // We do this until we reach the root of the tree, or we find the symbol

            // Reverse the hierarchy so we start from the node itself
            hierarchy.Reverse();

            var symbolHierarchy = SymbolHierarchy(node);

            if (symbolHierarchy.Count == 0)
            {
                return null;
            }

            symbolHierarchy.Reverse();

            var currentSymbol = symbolHierarchy[0];

            while (currentSymbol != null)
            {
                var foundSymbol = currentSymbol.LookupSymbol(name);

                if (foundSymbol != null)
                {
                    return foundSymbol;
                }

                currentSymbol = currentSymbol.Parent;
            }

            // Not in the current hierarchy. This means we can have three cases:
            // - Does in fact not exist
            // - Is a type in another module
            // - Is another module name

            return null;
        }

        public TypeSymbol? FindTypeBy(FileNode context, TypeReferenceNode node, ModuleSymbol? module)
        {
            return FindTypeBy(context, node.Name, module);
        }

        public TypeSymbol? FindTypeBy(FileNode context, string name, ModuleSymbol? module)
        {
            var imported = GetImportedModules(context);
            TypeSymbol? symbol = null;

            if (module != null)
            {
                // Use dictionary lookup for TypeSymbol by name
                var candidates = module.LookupAllSymbols(name);
                symbol = candidates.OfType<TypeSymbol>().FirstOrDefault();

                if (symbol == null)
                {
                    foreach (var type in module.Symbols.OfType<ModuleSymbol>())
                    {
                        symbol = FindTypeBy(context, name, type);

                        if (symbol != null)
                        {
                            return symbol;
                        }
                    }
                }

                return symbol;
            }

            // Search across imported modules using dictionary lookup
            foreach (var mod in imported)
            {
                var candidates = mod.LookupAllSymbols(name);
                symbol = candidates.OfType<TypeSymbol>().FirstOrDefault();
                if (symbol != null)
                {
                    return symbol;
                }
            }

            if (symbol == null)
            {
                foreach (var mod in Modules)
                {
                    symbol = FindTypeBy(context, name, mod);

                    if (symbol != null)
                    {
                        return symbol;
                    }
                }
            }

            return symbol;
        }

        private string? GetNodeName(INode node)
        {
            if (node is TypeReferenceNode type)
            {
                return type.Name;
            }
            else if (node is IdentifierNode id)
            {
                return id.Value;
            }
            else if (node is FuncCallNode funcId)
            {
                return ((IdentifierNode)funcId.Target).Value;
            }
            else if (node is ParameterNode param)
            {
                return param.Name;
            }
            else if (node is PropertyNode prop)
            {
                return prop.Name;
            }
            else if (node is VariableNode var)
            {
                return var.Name;
            }

            return null;
        }

        private List<ISymbol> SymbolHierarchy(INode node)
        {
            var astHierarchy = node.Hierarchy();

            // Filenode + First actual node
            if (astHierarchy.Count < 2)
            {
                Console.WriteLine("Failed to find symbol");
                return [];
            }

            // Drop the file node
            astHierarchy.RemoveAt(0);

            var symbolHierarchy = new List<ISymbol>();

            var currentNode = astHierarchy[0];
            // Get all modules from all assemblies and select the one that matches the current node
            ModulesByName.TryGetValue(((ModuleNode)currentNode).Name, out var moduleSymbol);
            ISymbol? currentSymbol = moduleSymbol;

            if (currentSymbol == null)
            {
                Console.WriteLine("Failed to find symbol");
                return [];
            }

            symbolHierarchy.Add(currentSymbol);
            astHierarchy.RemoveAt(0);

            currentNode = astHierarchy[0];

            while (currentNode != null && currentSymbol != null)
            {
                if (currentNode is BlockNode)
                {
                   // We skip Block Nodes as they have no representation in the symbol table
                   astHierarchy.RemoveAt(0);

                   if (astHierarchy.Count == 0)
                   {
                       break;
                   }

                   currentNode = astHierarchy[0];

                   continue;
                }
                else if (currentNode is InitNode init)
                {
                    // We need to find the init symbol that matches the init node by parameters
                    currentSymbol = currentSymbol.Symbols.FirstOrDefault(sym =>
                    {
                        if (sym is InitSymbol initSym)
                        {
                            return MatchParameters(node.Root, initSym, init.Parameters);
                        }
                        else
                        {
                            return false;
                        }
                    });
                }
                else if (currentNode is FuncNode func)
                {
                    // We need to find the function symbol that matches the function node by parameters
                    currentSymbol = currentSymbol.Symbols.FirstOrDefault(sym =>
                    {
                        if (sym is FuncSymbol funcSym)
                        {
                            var parameters = funcSym.LookupSymbol(node.ToString()) is ParameterSymbol;

                            if (parameters)
                            {
                                return true;
                            }

                            var variable = funcSym.LookupSymbol(node.ToString()) is VariableSymbol;
                            if (variable)
                            {
                                return true;
                            }
                        }

                        return false;
                    });
                }
                else if (currentNode is OperatorNode op)
                {
                    // We need to find the operator symbol that matches the operator node by parameters
                    currentSymbol = currentSymbol.Symbols.FirstOrDefault(sym =>
                    {
                        if (sym is OperatorSymbol opSym)
                        {
                            return MatchParameters(node.Root, opSym, op.Parameters);
                        }
                        else
                        {
                            return false;
                        }
                    });
                }
                else if (currentNode is ITypeNode type)
                {
                    // Use dictionary lookup for type by name, then filter by TypeSymbol
                    var candidates = currentSymbol.LookupAllSymbols(type.Name);
                    currentSymbol = candidates.OfType<TypeSymbol>().FirstOrDefault();
                }
                else if (currentNode is PropertyNode prop)
                {
                    var candidates = currentSymbol.LookupAllSymbols(prop.Name);
                    currentSymbol = candidates.OfType<PropertySymbol>().FirstOrDefault();
                }
                else
                {
                    // We hit a node that does not create a scope, thus we end the search
                    break;
                }

                if (currentSymbol != null)
                {
                    symbolHierarchy.Add(currentSymbol);
                    astHierarchy.RemoveAt(0);

                    if (astHierarchy.Count == 0)
                    {
                        break;
                    }

                    currentNode = astHierarchy[0];
                }
                else
                {
                    return symbolHierarchy;
                }
            }

            return symbolHierarchy;
        }

        private bool MatchParameters(FileNode context, ISymbol target, List<ParameterNode> nodeParams)
        {
            var parameters = target.Symbols.OfType<ParameterSymbol>().ToList();

            if (parameters.Count != nodeParams.Count)
            {
                return false;
            }

            for (int i = 0; i < parameters.Count; i++)
            {
                // There are three different cases to consider:
                // - The parameter is a type reference -> we need to compare the type name
                // - The parameter is an array -> we need to compare the type name of the array
                // - The parameter is a generic type -> we need to compare the type name of the generic type
                if (!MatchType(context, parameters[i].Type, nodeParams[i].TypeNode))
                {
                    return false;
                }
            }

            return true;
        }

        private bool MatchType(FileNode context, ITypeSymbol symbol, ITypeReferenceNode node)
        {
            var imported = GetImportedModules(context);

            if (symbol.IsConcrete && node is TypeReferenceNode type)
            {
                var typeSymbol = symbol as TypeSymbol;

                if (typeSymbol == null)
                {
                    return false;
                }

                var fqnMatches = typeSymbol.FullyQualifiedName == type.FullyQualifiedName;

                // If the fully qualified name matches, we have certainly found the symbol
                if (fqnMatches)
                {
                    return true;
                }

                var imports = ((FileNode)type.Root).Children.OfType<ImportNode>().Select(import => import.Name).ToList();

                var importedModules = new List<ModuleSymbol>();

                foreach (var import in imports)
                {
                    importedModules.AddRange(imported.Where(m => m.Name == import));
                }

                // If the fully qualified name doesn't match, we need to check in each module if the type is defined
                foreach (var module in importedModules)
                {
                    // Use dictionary lookup
                    var candidates = module.LookupAllSymbols(type.Name);
                    var foundSymbol = candidates.OfType<TypeSymbol>().FirstOrDefault();

                    if (foundSymbol != null)
                    {
                        type.FullyQualifiedName = foundSymbol.FullyQualifiedName;
                        return true;
                    }
                }

                return false;
            }

            return false;
        }

        public Result<TypeSymbol, SymbolResolutionError> FindType(FileNode context, string name)
        {
            var fqnType = FindTypeByFQN(context, name);

            if (fqnType != null)
            {
                return Result<TypeSymbol, SymbolResolutionError>.Ok(fqnType);
            }

            var simpleType = FindTypeBySimpleName(context, name);

            return simpleType;
        }

        public TypeSymbol? FindTypeByFQN(FileNode context, string name)
        {
            ISymbol? symbol = FindModuleByFQN(context, name);

            if (symbol == null)
            {
                return null;
            }

            string typePart = name.Remove(0, symbol.Name.Length + 1);

            var typeSplit = typePart.Split(".");

            if (typeSplit.Length == 0)
            {
                return null;
            }

            while (typeSplit.Length > 0)
            {
                symbol = symbol.LookupSymbol(typeSplit[0]);

                if (symbol == null)
                {
                    return null;
                }

                typeSplit = typeSplit.Skip(1).ToArray();
            }

            if (symbol is TypeSymbol typeSymbol)
            {
                return typeSymbol;
            }

            return null;
        }

        public TypeSymbol? FindTypeByFQN(string name)
        {
            ISymbol? symbol = FindModuleByFQN(name);

            if (symbol == null)
            {
                return null;
            }

            string typePart = name.Remove(0, symbol.Name.Length + 1);

            var typeSplit = typePart.Split(".");

            if (typeSplit.Length == 0)
            {
                return null;
            }

            while (typeSplit.Length > 0)
            {
                symbol = symbol.LookupSymbol(typeSplit[0]);

                if (symbol == null)
                {
                    return null;
                }

                typeSplit = typeSplit.Skip(1).ToArray();
            }

            if (symbol is TypeSymbol typeSymbol)
            {
                return typeSymbol;
            }

            return null;
        }

        public Result<TypeSymbol, SymbolResolutionError> FindTypeBySimpleName(FileNode context, string name)
        {
            var results = FindTypeByName(context, name, null);

            if (results.Count > 1)
            {
                // Prefer builtin types when there's ambiguity (e.g., Iona.Builtins.Void vs System.Void)
                var builtin = results.FirstOrDefault(t => t.FullyQualifiedName.StartsWith("Iona.Builtins."));
                if (builtin != null)
                {
                    return Result<TypeSymbol, SymbolResolutionError>.Ok(builtin);
                }
            }

            return results.Count switch
            {
                0 => Result<TypeSymbol, SymbolResolutionError>.Err(SymbolResolutionError.NotFound),
                > 1 => Result<TypeSymbol, SymbolResolutionError>.Err(SymbolResolutionError.Ambigious),
                _ => Result<TypeSymbol, SymbolResolutionError>.Ok(results[0])
            };
        }

        public ModuleSymbol? FindModuleByFQN(FileNode context, string name)
        {
            // First, break the fully qualified name into parts
            var parts = name.Split('.');

            if (parts.Length == 0)
            {
                return null;
            }

            // We need to find the module of thye fqn first.
            // Edge case: Modules can also have multiple parts in their name (e.g. std.io)
            // So we check the fqn minus the last part, then minus the second last part, etc. until we find a module
            var moduleName = parts.Aggregate((current, next) => current + "." + next);

            var imported = GetImportedModules(context.Root);
            ModuleSymbol? module = imported.FirstOrDefault(mod => mod.Name == moduleName);

            while (module == null && parts.Length > 1)
            {
                parts = parts.Take(parts.Length - 1).ToArray();
                moduleName = parts.Aggregate((current, next) => current + "." + next);
                // Use dictionary lookup for modules
                ModulesByName.TryGetValue(moduleName, out module);
            }

            if (module == null)
            {
                return null;
            }

            return module;
        }

        public ModuleSymbol? FindModuleByFQN(string name)
        {
            // First, break the fully qualified name into parts
            var parts = name.Split('.');

            if (parts.Length == 0)
            {
                return null;
            }

            // We need to find the module of thye fqn first.
            // Edge case: Modules can also have multiple parts in their name (e.g. std.io)
            // So we check the fqn minus the last part, then minus the second last part, etc. until we find a module
            var moduleName = parts.Aggregate((current, next) => current + "." + next);

            // Use dictionary lookup
            ModulesByName.TryGetValue(moduleName, out var module);

            while (module == null && parts.Length > 1)
            {
                parts = parts.Take(parts.Length - 1).ToArray();
                moduleName = parts.Aggregate((current, next) => current + "." + next);
                ModulesByName.TryGetValue(moduleName, out module);
            }

            if (module == null)
            {
                return null;
            }

            return module;
        }

        private List<TypeSymbol> FindTypeByName(FileNode context, string name, ISymbol? parent = null)
        {
            List<TypeSymbol> results = new();

            List<ISymbol> querySymbols;

            if (parent != null)
            {
                querySymbols = [parent];
            }
            else
            {
                var imported = GetImportedModules(context);
                querySymbols = [..imported];
            }

            foreach (var target in querySymbols)
            {
                // Use dictionary lookup
                var candidates = target.LookupAllSymbols(name);
                var found = candidates.OfType<TypeSymbol>().FirstOrDefault();

                if (found != null)
                {
                    results.Add(found);
                }

                foreach (var subtarget in target.Symbols.OfType<ModuleSymbol>())
                {
                    results.AddRange(FindTypeByName(context, name, subtarget));
                }
            }

            return results;
        }

        public List<ModuleSymbol> GetImportedModules(INode node)
        {
            var modules = new List<ModuleSymbol>();

            // All imported modules
            var reachableModules = node.Root.Children.OfType<ImportNode>().ToList();

            // Add the module the node is part of as well
            reachableModules.Add(new ImportNode(node.Module.Name, node.Root));

            foreach (var import in reachableModules)
            {
                if (ResolveImportedModule(import.Name) is ModuleSymbol module)
                {
                    modules.Add(module);
                    continue;
                }

                // TODO: Add error for imported module that doesnt exist
                Console.WriteLine($"Import not found {import.Name}");
            }

            return modules;
        }

        // Resolve a dotted import (e.g. "Iona.Builtins") to the deepest matching module, so
        // free-function lookup can target that module directly instead of recursing the forest.
        private ModuleSymbol? ResolveImportedModule(string name)
        {
            var parts = name.Split('.');
            var prefixLen = parts.Length;
            ModuleSymbol? module = null;

            while (prefixLen >= 1)
            {
                var prefix = string.Join(".", parts.Take(prefixLen));
                if (ModulesByName.TryGetValue(prefix, out module))
                {
                    break;
                }
                prefixLen--;
            }

            if (module == null)
            {
                return null;
            }

            for (var i = prefixLen; i < parts.Length; i++)
            {
                var child = module.Symbols.OfType<ModuleSymbol>().FirstOrDefault(m => m.Name == parts[i]);
                if (child == null)
                {
                    break;
                }
                module = child;
            }

            return module;
        }

        public Result<FuncSymbol, SymbolResolutionResult> CheckIfFuncExists(FileNode context, TypeSymbol? type, FuncCallNode funcCallNode, ISymbol? parent = null)
        {
            return FindFunc(context, type, funcCallNode);
        }

        public Result<InitSymbol, SymbolResolutionResult> CheckIfInitExists(FileNode context, InitCallNode initCallNode)
        {
            return FindInit(context, initCallNode);
        }

        public bool ArgsMatchParameters(List<ParameterSymbol> parameters, List<FuncCallArg> args)
        {
            bool matchingArgs = true;
            for (var x = 0; x < parameters.Count; x++)
            {
                if (
                    parameters[x].Type.FullyQualifiedName != args[x].Value.ResultType.FullyQualifiedName ||
                    parameters[x].Name != args[x].Name
                )
                {
                    matchingArgs = false;
                    break;
                }
            }

            return matchingArgs;
        }

        public Result<FuncSymbol, SymbolResolutionResult> FindFunc(FileNode context, TypeSymbol? type, FuncCallNode node)
        {
            List<FuncSymbol> results = new();

            if (type != null)
            {
                results = FindFuncs(type, node);

                if (results.Any())
                {
                    return Result<FuncSymbol, SymbolResolutionResult>.Ok(results.First());
                }

                if (type.BaseType != null)
                {
                    results.AddRange(FindFuncs(type.BaseType, node));
                }

                if (results.Any())
                {
                    return Result<FuncSymbol, SymbolResolutionResult>.Ok(results.First());
                }
            }

            if (results.Any())
            {
                return Result<FuncSymbol, SymbolResolutionResult>.Ok(results.First());
            }

            var imports = GetImportedModules(context);

            foreach (var import in imports)
            {
                results.AddRange(FindFuncs(import, node));
            }

            if (results.Count > 1)
            {
                return Result<FuncSymbol, SymbolResolutionResult>.Err(new SymbolResolutionResult
                {
                    Error = SymbolResolutionError.Ambigious,
                    Ambiguity = results.Select(func => func.Parent!.ToString() ?? "").ToList()
                });
            }

            if (results.Count == 1)
            {
                return Result<FuncSymbol, SymbolResolutionResult>.Ok(results.First());
            }

            return Result<FuncSymbol, SymbolResolutionResult>.Err(new SymbolResolutionResult
            {
                Error = SymbolResolutionError.NotFound,
                Ambiguity = []
            });
        }

        // Treat `argFqn` as assignable to `paramType` when they match exactly or when the
        // argument's type chain (BaseType walk) reaches the param's type — i.e. ordinary
        // subtype polymorphism through the inheritance chain.
        private bool IsAssignableTo(string argFqn, TypeSymbol paramType)
        {
            if (argFqn == paramType.FullyQualifiedName)
            {
                return true;
            }

            // Generic-parameter slot: at the call site we don't enforce the bound yet, so any
            // argument type satisfies a `T`-typed parameter. Constraint checking lands later.
            if (paramType.TypeKind == TypeKind.Generic)
            {
                return true;
            }

            // Walk the inheritance chain of the argument type.
            var argType = ModulesByName.Values
                .SelectMany(m => CollectAllTypes(m))
                .FirstOrDefault(t => t.FullyQualifiedName == argFqn);

            var current = argType?.BaseType;
            while (current != null)
            {
                if (current.FullyQualifiedName == paramType.FullyQualifiedName)
                {
                    return true;
                }
                current = current.BaseType;
            }

            return false;
        }

        private static IEnumerable<TypeSymbol> CollectAllTypes(ISymbol symbol)
        {
            foreach (var child in symbol.Symbols)
            {
                if (child is TypeSymbol t)
                {
                    yield return t;
                }
                if (child is ModuleSymbol or TypeSymbol)
                {
                    foreach (var nested in CollectAllTypes(child))
                    {
                        yield return nested;
                    }
                }
            }
        }

        private List<FuncSymbol> FindFuncs(ISymbol? symbol, FuncCallNode node)
        {
            List<FuncSymbol> results = new();

            // Use dictionary lookup to narrow down to funcs with matching name first
            var candidates = symbol.LookupAllSymbols(node.Target.Value);

            results
                .AddRange(
                    candidates
                        .OfType<FuncSymbol>()
                        .Where(func =>
                        {
                            if (node.Args.Count() != func.Symbols.OfType<ParameterSymbol>().Count())
                            {
                                return false;
                            }

                            bool matching = true;

                            foreach (var (param, arg) in func
                                         .Symbols
                                         .OfType<ParameterSymbol>()
                                         .Zip(node.Args, (p, a) => (p, a)))
                            {
                                if (arg.Value.ResultType == null || param.Name != arg.Name)
                                {
                                    matching = false;
                                    break;
                                }
                                if (!IsAssignableTo(arg.Value.ResultType.FullyQualifiedName, param.Type))
                                {
                                    matching = false;
                                    break;
                                }
                            }

                            return matching;

                        })
                );

            return results;
        }

        public Result<InitSymbol, SymbolResolutionResult> FindInit(FileNode context, InitCallNode node)
        {
            List<InitSymbol> results = new();

            var imports = GetImportedModules(context);

            foreach (var import in imports)
            {
                // Use dictionary lookup for types with the matching name
                var candidates = import.LookupAllSymbols(node.TypeFullName);
                var types = candidates.OfType<TypeSymbol>();

                foreach (var type in types)
                {
                    var inits = FindInits(type, node);
                    results.AddRange(inits);
                }
            }

            if (results.Count > 1)
            {
                return Result<InitSymbol, SymbolResolutionResult>.Err(new SymbolResolutionResult
                {
                    Error = SymbolResolutionError.Ambigious,
                    Ambiguity = results.Select(func => func.Parent!.ToString() ?? "").ToList()
                });
            }

            if (results.Count == 1)
            {
                return Result<InitSymbol, SymbolResolutionResult>.Ok(results.First());
            }

            return Result<InitSymbol, SymbolResolutionResult>.Err(new SymbolResolutionResult
            {
                Error = SymbolResolutionError.NotFound,
                Ambiguity = []
            });
        }

        private List<InitSymbol> FindInits(TypeSymbol? symbol, InitCallNode node)
        {
            List<InitSymbol> results = new();

            // Use dictionary lookup to get init symbols
            var candidates = symbol.LookupAllSymbols("init");

            results
                .AddRange(
                    candidates
                        .OfType<InitSymbol>()
                        .Where(func =>
                        {
                            if (func.Symbols.OfType<ParameterSymbol>().Count() != node.Args.Count)
                            {
                                return false;
                            }

                            foreach (var (param, arg) in func
                                         .Symbols
                                         .OfType<ParameterSymbol>()
                                         .Zip(node.Args, (p, a) => (p, a)))
                            {
                                if (param.Name != arg.Name)
                                {
                                    return false;
                                }
                                // Generic-parameter slots accept any argument type — bounds
                                // will be enforced later.
                                if (param.Type.TypeKind == TypeKind.Generic) { continue; }
                                if (param.Type.FullyQualifiedName != arg.Value.ResultType!.FullyQualifiedName)
                                {
                                    return false;
                                }
                            }

                            return true;

                        })
                );

            return results;
        }
    }
}
