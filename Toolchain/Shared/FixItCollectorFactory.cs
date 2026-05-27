//|--- FixItCollectorFactory.cs --------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

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
