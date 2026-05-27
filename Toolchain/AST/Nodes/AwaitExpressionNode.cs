//|--- AwaitExpressionNode.cs ----------------------------------|
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
    public class AwaitExpressionNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public TypeReferenceNode? ResultType { get; set; }
        public ExpressionType ExpressionType => ExpressionType.Await;
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public IExpressionNode Expression { get; set; }

        public AwaitExpressionNode(IExpressionNode expression, INode? parent = null)
        {
            Expression = expression;
            Parent = parent;
            Type = NodeType.Expression;
        }

        public void Accept(IAwaitExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
