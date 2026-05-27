//|--- BreakNode.cs --------------------------------------------|
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
    public class BreakNode : IStatementNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public StatementType StatementType { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public BreakNode(INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.BreakStatement;
            StatementType = StatementType.Break;
        }

        public void Accept(IBreakVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
