//|--- IAccessLevelNode.cs -------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Types;

namespace AST.Nodes
{
    public interface IAccessLevelNode : INode
    {
        AccessLevel AccessLevel { get; set; }
    }
}
