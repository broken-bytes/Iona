//|--- IType.cs ------------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace AST.Types
{
    public interface IType
    {
        public string Name { get; }
        public string Module { get; set; }
        public Kind TypeKind { get; set; }
    }
}
