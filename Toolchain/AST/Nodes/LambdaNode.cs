//|--- LambdaNode.cs -------------------------------------------|
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
    // `|p1, p2: Type| -> R { ... }` or `|p| expr`. Param types and return type may be omitted
    // when inferable from the expected function-type context (the slot the lambda is passed
    // into). The body is either a BlockNode or a bare IExpressionNode (implicit-return form).
    public class LambdaNode : IExpressionNode
    {
        public List<ParameterNode> Parameters { get; set; } = new();
        public TypeReferenceNode? ReturnType { get; set; }
        public INode? Body { get; set; }
        // Names of outer-scope locals/parameters referenced inside the body. Filled by typeck.
        public List<string> Captures { get; set; } = new();
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public ExpressionType ExpressionType => ExpressionType.Literal;
        public TypeReferenceNode? ResultType { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public LambdaNode(INode? parent = null)
        {
            Parent = parent;
            Type = NodeType.Lambda;
        }

        public void Accept(ILambdaVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
