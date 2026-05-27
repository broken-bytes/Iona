//|--- IfNode.cs -----------------------------------------------|
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
    public class IfNode : IStatementNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public StatementType StatementType { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public IExpressionNode Condition { get; set; }

        public BlockNode Body { get; set; }

        public List<ElseClause> ElseClauses { get; set; } = new();

        public IfNode(IExpressionNode condition, BlockNode body, INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.IfStatement;
            StatementType = StatementType.If;
            Condition = condition;
            Body = body;
        }

        public void Accept(IIfVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class ElseClause
    {
        public IExpressionNode? Condition { get; set; }

        public BlockNode Body { get; set; }

        public ElseClause(IExpressionNode? condition, BlockNode body)
        {
            Condition = condition;
            Body = body;
        }
    }
}
