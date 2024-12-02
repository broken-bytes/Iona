using AST.Nodes;
using Symbols;

namespace Typeck.Passes;

public class AssemblyPass : ISemanticAnalysisPass
{
    private AssemblyResolver _resolver;

    internal AssemblyPass(AssemblyResolver resolver)
    {
        _resolver = resolver;
    }
    
    public void Run(INode root, SymbolTable table)
    {
        
    }
}