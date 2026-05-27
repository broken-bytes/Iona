//|--- IStatementNode.cs ---------------------------------------|
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
    public interface IStatementNode : INode
    {
        public StatementType StatementType { get; set; }
    }
}
