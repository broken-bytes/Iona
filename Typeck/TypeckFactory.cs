namespace Typeck
{
    public static class TypeckFactory
    {
        public static ITypeck Create()
        {
            return new Typeck();
        }
    }
}
