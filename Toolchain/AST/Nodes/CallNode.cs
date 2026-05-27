//|--- CallNode.cs ---------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public interface ICallNode : IExpressionNode
    {
        public List<FuncCallArg> Args { get; set; }
    }
}