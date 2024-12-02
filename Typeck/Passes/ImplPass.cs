using AST.Nodes;
using Symbols;

namespace Typeck.Passes;

public class ImplPass : ISemanticAnalysisPass
{
    private ExpressionResolver _expressionResolver;
    private MutabilityResolver _mutabilityResolver;
    private TypeResolver _typeResolver;

    internal ImplPass(
        ExpressionResolver expressionResolver,
        MutabilityResolver mutabilityResolver,
        TypeResolver typeResolver
    )
    {
        _expressionResolver = expressionResolver;
        _mutabilityResolver = mutabilityResolver;
        _typeResolver = typeResolver;
    }
    
    public void Run(INode root, SymbolTable table)
    {
        
    }
}