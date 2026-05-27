//|--- GeneratorFactory.cs -------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

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
