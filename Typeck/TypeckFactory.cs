using Shared;
using Typeck.Passes;
using Typeck.Passes.Decl;
using Typeck.Passes.Impl;

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
            var assemblyResolver = new AssemblyResolver();
            var tableConstructor = new SymbolTableConstructor();
            var topLevelResolver = new TopLevelScopeResolver(errorCollector, warningCollector, fixItCollector);
            var typeResolver = new TypeResolver(errorCollector, warningCollector, fixItCollector);
            var expressionResolver = new ExpressionResolver(errorCollector);
            var mutabilityResolver = new MutabilityResolver();
            
            var declPass = DeclPassFactory.Create(expressionResolver, errorCollector);
            var implPass = ImplPassFactory.Create(expressionResolver, mutabilityResolver, typeResolver);

            return new Typeck(
                declPass,
                implPass,
                assemblyResolver,
                tableConstructor, 
                topLevelResolver, 
                typeResolver, 
                expressionResolver,
                mutabilityResolver
            );
        }
    }
}
