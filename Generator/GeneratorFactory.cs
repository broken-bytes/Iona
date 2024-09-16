namespace Generator
{
    public static class GeneratorFactory
    {
        public static IGenerator Create()
        {
            return new Generator();
        }
    }
}
