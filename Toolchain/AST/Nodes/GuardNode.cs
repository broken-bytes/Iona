//|--- GuardNode.cs --------------------------------------------|
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
    public class GuardNode : IStatementNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public StatementType StatementType { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public IExpressionNode? Condition { get; set; }

        public BlockNode Body { get; set; }

        public string? BindingName { get; set; }

        public bool BindingIsMutable { get; set; }

        public IExpressionNode? BindingExpression { get; set; }

        public TypeReferenceNode? BindingTypeNode { get; set; }

        public GuardNode(IExpressionNode condition, BlockNode body, INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.GuardStatement;
            StatementType = StatementType.Guard;
            Condition = condition;
            Body = body;
        }

        public GuardNode(string bindingName, bool isMutable, IExpressionNode bindingExpression, BlockNode body, INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.GuardStatement;
            StatementType = StatementType.Guard;
            BindingName = bindingName;
            BindingIsMutable = isMutable;
            BindingExpression = bindingExpression;
            Body = body;
        }

        public void Accept(IGuardVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
