//|--- IASTVisualizer.cs ---------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using System;
using System.Collections.Generic;
using AST.Nodes;

namespace ASTVisualizer
{
    public interface IASTVisualizer
    {
        public string Visualize(INode node);
    }
}
