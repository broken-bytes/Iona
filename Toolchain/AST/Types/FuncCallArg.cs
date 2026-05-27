//|--- FuncCallArg.cs ------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Nodes;

namespace AST.Types
{
    public class FuncCallArg(string name, IExpressionNode value)
    {
        public string Name = name;
        public IExpressionNode Value = value;
    }
}
