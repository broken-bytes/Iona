using AST.Nodes;
using Symbols;

namespace Typeck
{
    internal class ExpressionScopeResolver
    {
        private SymbolTable table;

        internal ExpressionScopeResolver()
        {
            table = new SymbolTable();
        }

        internal void CheckScopes(INode ast, SymbolTable table)
        {
            
        }
    }
}
