﻿using AST.Types;
using AST.Visitors;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class SelfNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public ExpressionType ExpressionType => ExpressionType.Identifier;
        public INode? ResultType { get; set; }
        public INode Root { get => Utils.GetRoot(this); }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public SelfNode(INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.Identifier;
        }

        public void Accept(ISelfVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return "self";
        }
    }
}
