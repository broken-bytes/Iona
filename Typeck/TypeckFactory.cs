namespace Typeck
{
    public static class TypeckFactory
    {
        public static ITypeck Create()
        {
            var tableConstructor = new SymbolTableConstructor();
            var scopeChecker = new ScopeChecker();
            var typeChecker = new TypeChecker();

            return new Typeck(tableConstructor, scopeChecker, typeChecker);
        }
    }
}
