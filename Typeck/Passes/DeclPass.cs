using AST.Nodes;
using Symbols;

namespace Typeck.Passes;

public class DeclPass : ISemanticAnalysisPass
{
    private ExpressionResolver _expressionResolver;
    private TopLevelScopeResolver _topLevelScopeResolver;

    internal DeclPass(
        ExpressionResolver resolver,
        TopLevelScopeResolver topLevelScopeResolver
    )
    {
        _expressionResolver = resolver;
        _topLevelScopeResolver = topLevelScopeResolver;
    }
    
    public void Run(INode root, SymbolTable table)
    {
        
    }
}