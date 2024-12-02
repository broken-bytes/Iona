using AST.Nodes;
using Symbols;

namespace Typeck.Passes;

public class DeclPass : ISemanticAnalysisPass
{
    private readonly List<ISemanticAnalysisPass> _subPasses;
    
    internal DeclPass(List<ISemanticAnalysisPass> subPasses)
    {
        _subPasses = subPasses;
    }
    
    public void Run(FileNode root, SymbolTable table, string assemblyName)
    {
        foreach (var pass in _subPasses)
        {
            pass.Run(root, table, assemblyName);
        }
    }
}