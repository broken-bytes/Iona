//|--- ITypeSymbol.cs ------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace Symbols.Symbols
{
    public interface ITypeSymbol : ISymbol
    {
        public bool IsArray { get; }
        public bool IsConcrete { get; }
        public bool IsGeneric { get; }
    }
}
