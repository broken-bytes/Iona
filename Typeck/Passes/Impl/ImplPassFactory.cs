namespace Typeck.Passes.Impl;

public static class ImplPassFactory
{
    internal static ImplPass Create(
        ExpressionResolver expressionResolver,
        MutabilityResolver mutabilityResolver,
        TypeResolver typeResolver
    )
    {
        var bodyPass = new ImplPassBodyChecksSubPass();
        var initPass = new ImplPassInitialisationSubPass();
        var mutPass = new ImplPassMutabilitySubPass();
        
        return new ImplPass([bodyPass, initPass, mutPass]);
    }
}