namespace Typeck
{
    public static class TypeckFactory
    {
        public static ITypeck Create()
        {
            var tableConstructor = new SymbolTableConstructor();
            var topLevelResolver = new TopLevelScopeResolver();
            var typeResolver = new TypeResolver();
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
