//|--- InterpolatedStringNode.cs -------------------------------|
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
    public class InterpolatedStringNode : IExpressionNode
    {
        public List<INode> Segments { get; set; } = new();
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public ExpressionType ExpressionType => ExpressionType.Literal;
        public TypeReferenceNode? ResultType { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public InterpolatedStringNode(INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.InterpolatedString;
        }

        public void Accept(IInterpolatedStringVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
