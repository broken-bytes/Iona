﻿using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class IdentifierNode : IExpressionNode
    {
        public string Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public ExpressionType ExpressionType => ExpressionType.Identifier;
        public Types.Type? ResultType { get; set; }
        public INode Root { get => Utils.GetRoot(this); }

        public IdentifierNode(string name, INode? parent = null)
        {
            Name = name;
            Parent = parent;
            Type = NodeType.Identifier;
        }

        public void Accept(IIdentifierVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
