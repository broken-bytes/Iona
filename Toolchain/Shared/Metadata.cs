//|--- Metadata.cs ---------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace Shared
{
    public struct Metadata
    {
        public string File { get; set; }
        public int LineStart { get; set; }
        public int LineEnd { get; set; }
        public int ColumnStart { get; set; }
        public int ColumnEnd { get; set; }

        public override string ToString()
        {
            return $"{File}:{LineStart}:{ColumnStart}";
        }
    }
}
