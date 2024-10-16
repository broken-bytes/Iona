﻿using AST.Types;
using AST.Visitors;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class BinaryExpressionNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Left { get; set; }
        public INode Right { get; set; }
        public BinaryOperation Operation { get; set; }
        public ExpressionType ExpressionType { get; set; }
        public INode? ResultType { get; set; }
        public INode Root { get => Utils.GetRoot(this); }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public BinaryExpressionNode(
            INode left,
            INode right,
            BinaryOperation operation,
            INode? resultType = null,
            INode? parent = null
        )
        {
            Parent = parent;
            Type = NodeType.Assignment;
            Left = left;
            Right = right;
            Operation = operation;
            ResultType = resultType;
        }

        public void Accept(IBinaryExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
