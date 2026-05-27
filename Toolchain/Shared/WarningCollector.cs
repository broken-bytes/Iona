//|--- WarningCollector.cs -------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace Shared
{
    internal class WarningCollector : IWarningCollector
    {
        public List<CompilerWarning> Warnings { get; private set; }
        public bool HasWarning => Warnings.Any();

        internal WarningCollector()
        {
            Warnings = new List<CompilerWarning>();
        }

        public void Collect(CompilerWarning error)
        {
            Warnings.Add(error);
        }
    }
}
