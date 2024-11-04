using Symbols.Symbols;
using AST.Nodes;
using AST.Types;
using System.Xml.Linq;

namespace Symbols
{
    public class SymbolTable
    {
        public List<AssemblySymbol> Assemblies;

        public SymbolTable()
        {
            Assemblies = new List<AssemblySymbol>();
        }

        /// <summary>
        /// Returns the deepest symbol that matches the node
        /// Examples:
        /// - If the node is a variable, it will return the variable symbol
        /// - If the node doesn't create a symbol, it will return its parent symbol (e.g. a block)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
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
            // We do this until we reach the root of the tree or we find the symbol

            // Reverse the hierarchy so we start from the the node itself
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
                var foundSymbol = currentSymbol.Symbols.FirstOrDefault(symbol => symbol.Name == name);

                if (foundSymbol != null)
                {
                    return foundSymbol;
                }

                currentSymbol = currentSymbol.Parent;
            }

            return null;
        }

        /// <summary>
        /// Gets the type symbol this symbol is contained in.
        /// </summary>
        /// <param name="symbol"></param>
        /// <note>Free functions and types themselves do return null</note>
        /// <returns></returns>
        private ISymbol? GetContainedType(ISymbol symbol)
        {
            var currentSymbol = symbol;

            while (currentSymbol.Parent != null)
            {
                if (currentSymbol.Parent.Kind == SymbolKind.Type)
                {
                    return currentSymbol.Parent;
                }

                currentSymbol = currentSymbol.Parent;
            }

            return null;
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

        /// <summary>
        /// Creates the symbol hierarchy that matches the node's ast hierarchy
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private List<ISymbol> SymbolHierarchy(INode node)
        {
            var astHierarchy = node.Hierarchy();

            // Drop the file node
            astHierarchy.RemoveAt(0);

            var symbolHierarchy = new List<ISymbol>();

            var currentNode = astHierarchy[0];
            // Get all modules from all assemblies and select the one that matches the current node
            var modules = Assemblies.SelectMany(asm => asm.Symbols).OfType<ModuleSymbol>().ToList();
            ISymbol? currentSymbol = modules.FirstOrDefault(mod => mod.Name == ((ModuleNode)currentNode).Name);

            if (currentSymbol == null)
            {
                return [];
            }

            symbolHierarchy.Add(currentSymbol);
            astHierarchy.RemoveAt(0);

            currentNode = astHierarchy[0];

            while (currentNode != null && currentSymbol != null)
            {
                if (currentNode is BlockNode)
                {
                    currentSymbol = currentSymbol.Symbols.FirstOrDefault(sym => sym is BlockSymbol);
                }
                else if (currentNode is InitNode init)
                {
                    // We need to find the init symbol that matches the init node by parameters
                    currentSymbol = currentSymbol.Symbols.FirstOrDefault(sym =>
                    {
                        if (sym is InitSymbol initSym)
                        {
                            return MatchParameters(initSym, init.Parameters);
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
                            return MatchParameters(funcSym, func.Parameters);
                        }
                        else
                        {
                            return false;
                        }
                    });
                }
                else if (currentNode is OperatorNode op)
                {
                    // We need to find the operator symbol that matches the operator node by parameters
                    currentSymbol = currentSymbol.Symbols.FirstOrDefault(sym =>
                    {
                        if (sym is OperatorSymbol opSym)
                        {
                            return MatchParameters(opSym, op.Parameters);
                        }
                        else
                        {
                            return false;
                        }
                    });
                }
                else if (currentNode is ITypeNode type)
                {
                    currentSymbol = currentSymbol.Symbols.FirstOrDefault(sym => sym is TypeSymbol && sym.Name == type.Name);
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
                    return [];
                }
            }

            return symbolHierarchy;
        }

        private bool MatchParameters(ISymbol target, List<ParameterNode> nodeParams)
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
                if (!MatchType(parameters[i].Type, nodeParams[i].TypeNode))
                {
                    return false;
                }
            }

            return true;
        }

        private bool MatchType(ITypeSymbol symbol, ITypeReferenceNode node)
        {
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
                // Always add the current module to the list of imported modules if it's not already there (should be the case)
                var moduleNode = ((FileNode)type.Root).Children.OfType<ModuleNode>().FirstOrDefault();

                if (moduleNode != null)
                {
                    imports.Add(moduleNode.Name);
                }
                else
                {
                    return false;
                }

                var modules = Assemblies.SelectMany(asm => asm.Symbols).OfType<ModuleSymbol>().ToList();
                var importedModules = modules.Where(module => imports.First(import => import == module.Name) != null);

                // If the fully qualified name doesn't match, we need to check in each module if the type is defined
                foreach (var module in importedModules)
                {
                    var foundSymbol = module.Symbols.OfType<TypeSymbol>().FirstOrDefault(sym => sym.Name == type.Name);

                    if (foundSymbol != null)
                    {
                        type.FullyQualifiedName = foundSymbol.FullyQualifiedName;
                        return true;
                    }
                }

                return false;
            }

            if (symbol.IsArray && node is ArrayTypeReferenceNode array)
            {
                var arraySymbol = symbol as ArrayTypeSymbol;

                if (arraySymbol == null)
                {
                    return false;
                }

                // Match the element type
                return MatchType(arraySymbol.ElementType, array.ElementType);
            }

            if (symbol.IsGeneric && node is GenericTypeReferenceNode generic)
            {
                var genericSymbol = symbol as GenericTypeSymbol;

                if (genericSymbol == null)
                {
                    return false;
                }

                // Match the type arguments
                if (genericSymbol.TypeArguments.Count != generic.GenericArguments.Count)
                {
                    return false;
                }

                for (int i = 0; i < genericSymbol.TypeArguments.Count; i++)
                {
                    if (!MatchType(genericSymbol.TypeArguments[i], generic.GenericArguments[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public TypeSymbol? FindTypeByFQN(string name)
        {
            // First, find the correct assembly
            var assembly = FindAssemblyByFQN(name);

            // Remove the assembly name from the fqn
            var modulePart = name.Remove(0, assembly.Name.Length + 1);

            var module = FindModuleByFQN(assembly, modulePart);

            if (module == null)
            {
                return null;
            }

            string typePart = modulePart.Remove(0, module.Name.Length + 1);

            var typeSplit = typePart.Split(".");

            if (typeSplit.Length == 0)
            {
                return null;
            }

            TypeSymbol? type = module.Symbols.OfType<TypeSymbol>().FirstOrDefault(sym => sym.Name == typeSplit[0]);

            if (type == null)
            {
                return null;
            }

            typeSplit = typeSplit.Skip(1).ToArray();

            while (typeSplit.Length > 0)
            {
                var foundSymbol = type.Symbols.Find(sym => sym.Name == typeSplit[0]);

                if (foundSymbol is TypeSymbol foundType)
                {
                    type = foundType;
                    typeSplit = typeSplit.Skip(1).ToArray();
                }
                else
                {
                    return null;
                }
            }

            return type;
        }

        public AssemblySymbol? FindAssemblyByFQN(string name)
        {
            // First, break the fully qualified name into parts
            var parts = name.Split('.');

            if (parts.Length == 0)
            {
                return null;
            }

            // We need to find the assembly of the fqn first. 
            // Edge case: Modules can also have multiple parts in their name (e.g. std.io)
            // So we check the fqn minus the last part, then minus the second last part, etc. until we find a module
            var assemblyName = parts.Aggregate((current, next) => current + "." + next);

            AssemblySymbol? assembly = Assemblies.FirstOrDefault(assembly => assembly.Name == assemblyName);

            while (assembly == null && parts.Length > 1)
            {
                parts = parts.Take(parts.Length - 1).ToArray();
                assemblyName = parts.Aggregate((current, next) => current + "." + next);
                assembly = Assemblies.FirstOrDefault(assembly => assembly.Name == assemblyName);
            }

            if (assembly == null)
            {
                return null;
            }

            return assembly;
        }

        public ModuleSymbol? FindModuleByFQN(AssemblySymbol assembly, string name)
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

            var modules = assembly.Symbols.OfType<ModuleSymbol>();
            ModuleSymbol? module = modules.FirstOrDefault(mod => mod.Name == moduleName);

            while (module == null && parts.Length > 1)
            {
                parts = parts.Take(parts.Length - 1).ToArray();
                moduleName = parts.Aggregate((current, next) => current + "." + next);
                module = modules.FirstOrDefault(mod => mod.Name == moduleName);
            }

            if (module == null)
            {
                return null;
            }

            return module;
        }

        public ModuleSymbol? FindModuleByFQN(string name)
        {
            // First, find the correct assembly
            var assembly = FindAssemblyByFQN(name);
            if (assembly == null)
            {
                return null;
            }

            var modulePart = name.Remove(0, assembly.Name.Length + 1);

            return FindModuleByFQN(assembly, modulePart);
        }
    }
}
