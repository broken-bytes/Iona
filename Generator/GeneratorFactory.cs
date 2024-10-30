using Shared;

namespace Generator
{
    public static class GeneratorFactory
    {
        public static IGenerator Create(
            IErrorCollector errorCollector,
            IWarningCollector warningCollector,
            IFixItCollector fixItCollector
        )
        {
            return new Generator(errorCollector, warningCollector, fixItCollector);
        }
    }
}
