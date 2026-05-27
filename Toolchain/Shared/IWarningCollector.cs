//|--- IWarningCollector.cs ------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace Shared
{
    public interface IWarningCollector
    {
        public List<CompilerWarning> Warnings { get; }
        public bool HasWarning => Warnings.Any();

        public void Collect(CompilerWarning error);
    }
}