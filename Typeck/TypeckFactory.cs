using Shared;

namespace Typeck
{
    public static class TypeckFactory
    {
        public static ITypeck Create(IErrorCollector errorCollector)
        {
            var tableConstructor = new SymbolTableConstructor();
            var topLevelResolver = new TopLevelScopeResolver(errorCollector);
            var typeResolver = new TypeResolver(errorCollector);
            var expressionResolver = new ExpressionScopeResolver();
            var mutabilityResolver = new MutabilityResolver();

            return new Typeck(
                tableConstructor, 
                topLevelResolver, 
                typeResolver, 
                expressionResolver,
                mutabilityResolver
            );
        }
    }
}
