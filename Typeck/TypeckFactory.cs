using Shared;

namespace Typeck
{
    public static class TypeckFactory
    {
        public static ITypeck Create(
            IErrorCollector errorCollector,
            IWarningCollector warningCollector,
            IFixItCollector fixItCollector
        )
        {
            var tableConstructor = new SymbolTableConstructor();
            var topLevelResolver = new TopLevelScopeResolver(errorCollector, warningCollector, fixItCollector);
            var typeResolver = new TypeResolver(errorCollector, warningCollector, fixItCollector);
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
