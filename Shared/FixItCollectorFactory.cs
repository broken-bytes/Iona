namespace Shared
{
    public static class FixItCollectorFactory
    {
        public static IFixItCollector Create()
        {
            return new FixItCollector();
        }
    }
}
