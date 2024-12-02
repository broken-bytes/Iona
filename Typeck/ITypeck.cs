using System.Reflection;
using AST.Nodes;
using Symbols;
using Symbols.Symbols;

namespace Typeck
{
    public interface ITypeck
    {
        public void DoSemanticAnalysis(INode node, string assembly, SymbolTable symbolTable);
        public void AddImportedAssemblySymbols(SymbolTable table, List<string> assemblies);
    }
}
