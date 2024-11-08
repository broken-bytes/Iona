using System.Reflection;
using AST.Nodes;
using Symbols;
using Symbols.Symbols;

namespace Typeck
{
    public interface ITypeck
    {
        public SymbolTable BuildSymbolTable(INode node, string assembly);
        public void CheckTopLevelScopes(INode node, SymbolTable table);
        public void CheckExpressions(INode node, SymbolTable table);
        public void TypeCheck(INode node, SymbolTable table);
        public SymbolTable MergeTables(List<SymbolTable> tables, List<string> assemblies);
    }
}
