//|--- NodeKInd.cs ---------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace AST.Types
{
    public enum NodeKind
    {
        Array,
        Class,
        Contract,
        Enum,
        File,
        Module,
        Struct,
        Function,
        Variable,
        UnknownKind,
        Primitive,
    }
}
