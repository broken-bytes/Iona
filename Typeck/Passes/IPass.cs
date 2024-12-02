using AST.Nodes;
using Symbols;

namespace Typeck.Passes;

public interface ISemanticAnalysisPass
{
    public void Run(FileNode root, SymbolTable table, string assemblyName);
}