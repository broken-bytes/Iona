using Shared;

namespace Generator
{
    public static class GeneratorFactory
    {
        public static IGenerator Create(IErrorCollector errorCollector)
        {
            return new Generator(errorCollector);
        }
    }
}
