//|--- IProcessor.cs -------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using Lexer.Tokens;

namespace Lexer.Processors
{
    public interface IProcessor
    {
        Token? Process(string source);
    }
}
