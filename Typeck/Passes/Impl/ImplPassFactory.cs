using Shared;

namespace Typeck.Passes.Impl;

public static class ImplPassFactory
{
    internal static ImplPass Create(
        IErrorCollector errorCollector,
        ExpressionResolver expressionResolver,
        MutabilityResolver mutabilityResolver,
        TypeResolver typeResolver
    )
    {
        var bodyPass = new ImplPassBodyChecksSubPass(errorCollector, expressionResolver);
        var expressionPass = new ImplPassExpressionChecksSubPass(expressionResolver);
        var initPass = new ImplPassInitialisationSubPass();
        var mutPass = new ImplPassMutabilitySubPass();
        
        return new ImplPass([bodyPass, expressionPass, initPass, mutPass]);
    }
}