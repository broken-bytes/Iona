using Symbols.Symbols;
using AST.Nodes;

namespace Symbols
{
    public class SymbolTable
    {
        public List<ModuleSymbol> Modules;

        public SymbolTable()
        {
            Modules = new List<ModuleSymbol>();
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

            // The root is always a file node, and we know a file always contains only one module
            var moduleNode = hierarchy.OfType<ModuleNode>().FirstOrDefault();

            if (moduleNode == null)
            {
                return null;
            }

            // Then, find the module symbol that contains the node
            var module = Modules.Find(m => m.Name == moduleNode.Name);

            if (module == null)
            {
                return null;
            }

            // Traverse the hierarchy and find the symbol that matches the node(by name)
            ISymbol? currentSymbol = module;

            foreach (var child in hierarchy.Skip(1))
            {
                if (currentSymbol == null)
                {
                    return null;
                }

                if (child is ITypeNode type)
                {
                    currentSymbol = currentSymbol.Symbols.Find(s => s.Name == type.Name);
                }

                if (child is BlockNode block)
                {
                    currentSymbol = currentSymbol?.Symbols.OfType<BlockSymbol>().FirstOrDefault();
                }

                if (child is IdentifierNode identifier)
                {
                    currentSymbol = currentSymbol.Symbols.Find(s => s.Name == identifier.Name);
                }

                if (child is InitNode init)
                {
                    var inits = currentSymbol?.Symbols.OfType<InitSymbol>();

                    if (!inits.Any())
                    {
                        return null;
                    }

                    currentSymbol = inits.FirstOrDefault(symbol => {
                        var parameters = symbol.Symbols.OfType<ParameterSymbol>().ToList();

                        for (int x = 0; x < parameters.Count; x++)
                        {
                            if (
                                parameters[x].Name == init.Parameters[x].Name ||
                                parameters[x].Type.Name == ((TypeReferenceNode)init.Parameters[x].Type).Name
                            )
                            {
                                return true;
                            }

                        }

                        return false;
                    });
                }
            }

            return currentSymbol;
        }
    }
}
