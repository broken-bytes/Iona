//|--- IFixItCollector.cs --------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace Shared
{
    public interface IFixItCollector
    {
        public List<FixIt> FixIts { get; }
        public void Collect(FixIt fixit);
    }
}
