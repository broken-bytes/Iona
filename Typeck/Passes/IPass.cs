using AST.Nodes;
using Symbols;

namespace Typeck.Passes;

public interface ISemanticAnalysisPass
{
    public void Run(List<FileNode> files, SymbolTable table, string assemblyName);
}