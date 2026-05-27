//|--- ForNode.cs ----------------------------------------------|
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
    public class ForNode : IStatementNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public StatementType StatementType { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public string IteratorName { get; set; }

        public IExpressionNode Iterable { get; set; }

        public BlockNode Body { get; set; }

        public ForNode(string iteratorName, IExpressionNode iterable, BlockNode body, INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.ForLoop;
            StatementType = StatementType.For;
            IteratorName = iteratorName;
            Iterable = iterable;
            Body = body;
        }

        public void Accept(IForVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
