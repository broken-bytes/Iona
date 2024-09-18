namespace Typeck
{
    public static class TypeckFactory
    {
        public static ITypeck Create()
        {
            var tableConstructor = new SymbolTableConstructor();
            var typeChecker = new TypeChecker();

            return new Typeck(tableConstructor, typeChecker);
        }
    }
}
