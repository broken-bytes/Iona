//|--- IParser.cs ----------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Nodes;
using Lexer.Tokens;

namespace Parser
{
    public interface IParser
    {
        public INode Parse(TokenStream stream, string assemblyName);
    }
}
