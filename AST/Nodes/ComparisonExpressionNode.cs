﻿using AST.Types;
using AST.Visitors;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class ComparisonExpressionNode : INode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Left { get; set; }
        public INode Right { get; set; }
        public ComparisonOperation Operation { get; set; }
        public ExpressionType ExpressionType { get; set; }
        public INode Root { get => Utils.GetRoot(this); }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public ComparisonExpressionNode(
            INode left,
            INode right,
            ComparisonOperation operation,
            INode? parent = null
        )
        {
            Parent = parent;
            Type = NodeType.Assignment;
            Left = left;
            Right = right;
            Operation = operation;
        }

        public void Accept(IComparisonExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
