//|--- FixItFactory.cs -----------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace Shared
{
    public static class FixItFactory
    {
        public static FixIt ImportMissing(string import, Metadata meta)
        {
            return new FixIt {
                Message = $"Add missing import {import}",
                Code = $"import {import}",
                Meta = meta
            };
        }
    }
}
