//|--- InvokeExpressionNode.cs ---------------------------------|
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
    public class InvokeExpressionNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public IExpressionNode Callee { get; set; }
        public List<FuncCallArg> Args { get; set; } = [];
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }
        public ExpressionType ExpressionType => ExpressionType.FunctionCall;
        public TypeReferenceNode? ResultType { get; set; }

        public InvokeExpressionNode(IExpressionNode callee, INode? parent = null)
        {
            Type = NodeType.FuncCall;
            Parent = parent;
            Callee = callee;
        }

        public void Accept(IInvokeExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
