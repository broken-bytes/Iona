using AST.Nodes;
using Symbols;

namespace Typeck.Passes;

public interface ISemanticAnalysisPass
{
    public void Run(INode root, SymbolTable table);
}