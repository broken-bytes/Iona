﻿using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class IdentifierNode : IExpressionNode
    {
        public string Value { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public ExpressionType ExpressionType => ExpressionType.Identifier;
        public INode? ResultType { get; set; }
        public INode Root { get => Utils.GetRoot(this); }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public IdentifierNode(string value, INode? parent = null)
        {
            Value = value;
            Parent = parent;
            Type = NodeType.Identifier;
        }

        public void Accept(IIdentifierVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
