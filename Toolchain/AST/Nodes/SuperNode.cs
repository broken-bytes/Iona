//|--- SuperNode.cs --------------------------------------------|
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
    public class SuperNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public ExpressionType ExpressionType => ExpressionType.Self;
        public TypeReferenceNode? ResultType { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public SuperNode(INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.Self;
        }

        public override string ToString()
        {
            return "base";
        }

        public void Accept(ISuperVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
