using System.Reflection;
using AST.Nodes;
using Symbols;
using Symbols.Symbols;

namespace Typeck
{
    public interface ITypeck
    {
        public void BuildSymbolTable(INode node, string assembly, SymbolTable symbolTable);
        public void CheckTopLevelScopes(INode node, SymbolTable table);
        public void CheckExpressions(INode node, SymbolTable table);
        public void TypeCheck(INode node, SymbolTable table);
        public void AddImportedAssemblySymbols(SymbolTable table, List<string> assemblies);
    }
}
