using Symbols.Symbols;
using AST.Nodes;
using AST.Types;

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

            // First, we need to find the name of the symbol to be found
            var name = "";

            if (hierarchy.Count == 0)
            {
                return null;
            }

            var target = hierarchy[hierarchy.Count - 1];

            if (target is IdentifierNode id)
            {
                name = id.Name;
            }
            else if (target is FuncCallNode funcCall)
            {
                if (funcCall.Target is IdentifierNode funcId)
                {
                    name = funcId.Name;
                }
                else
                {
                    return null;
                }
            }
            else if (target is ParameterNode param)
            {
                if (param.TypeNode is TypeReferenceNode type)
                {
                    name = type.Name;
                }
                else
                {
                    return null;
                }
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

        private List<ISymbol> GetSymbolHierarchy(INode node)
        {
            var nodeHierarchy = node.Hierarchy();

            return [];
        } 
    }
}
