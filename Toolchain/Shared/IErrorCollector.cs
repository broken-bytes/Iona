//|--- IErrorCollector.cs --------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace Shared
{
    public interface IErrorCollector
    {
        public List<CompilerError> Errors { get; }
        public void Collect(CompilerError error);
    }
}
