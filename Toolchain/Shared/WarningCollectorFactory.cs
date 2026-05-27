//|--- WarningCollectorFactory.cs ------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public static class WarningCollectorFactory
    {
        public static IWarningCollector Create()
        {
            return new WarningCollector();
        }
    }
}
