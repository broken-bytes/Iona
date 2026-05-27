//|--- SymbolResolutionResult.cs -------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace Symbols;


public struct SymbolResolutionResult
{
    public SymbolResolutionError Error;
    public List<string> Ambiguity;
}

public enum SymbolResolutionError
{
    NotFound,
    Ambigious
}