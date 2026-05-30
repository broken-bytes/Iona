//|--- TypeOfExpressionNode.cs ---------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Types;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class TypeOfExpressionNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public TypeReferenceNode Operand { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }
        public ExpressionType ExpressionType => ExpressionType.Literal;
        public TypeReferenceNode? ResultType { get; set; }

        public TypeOfExpressionNode(TypeReferenceNode operand, INode? parent = null)
        {
            Type = NodeType.Literal;
            Parent = parent;
            Operand = operand;
        }
    }
}
