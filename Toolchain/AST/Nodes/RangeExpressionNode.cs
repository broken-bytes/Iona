//|--- RangeExpressionNode.cs ----------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Types;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class RangeExpressionNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public ExpressionType ExpressionType { get; set; }
        public TypeReferenceNode? ResultType { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public IExpressionNode Start { get; set; }

        public IExpressionNode End { get; set; }

        public RangeExpressionNode(IExpressionNode start, IExpressionNode end, INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.Expression;
            ExpressionType = ExpressionType.For; // Reuse For expression type for range
            Start = start;
            End = end;
        }
    }
}
