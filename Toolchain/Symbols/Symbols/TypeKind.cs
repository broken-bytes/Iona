//|--- TypeKind.cs ---------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace Symbols.Symbols
{
    public enum TypeKind
    {
        Class,
        Contract,
        Enum,
        Generic,
        Record,
        Struct,
        Primitive,
        Unknown
    }
}
