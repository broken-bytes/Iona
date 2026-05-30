//|--- MapLiteralNode.cs ---------------------------------------|
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
    // `["a": 1, "b": 2]` literal. Empty form is `[:]`. Lowered to System.Collections.Generic
    // .Dictionary<K, V> at codegen.
    public class MapLiteralNode : IExpressionNode
    {
        public List<IExpressionNode> Keys { get; set; } = new();
        public List<IExpressionNode> Values { get; set; } = new();
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public ExpressionType ExpressionType => ExpressionType.Literal;
        public TypeReferenceNode? ResultType { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public MapLiteralNode(INode? parent = null)
        {
            Type = NodeType.MapLiteral;
            Parent = parent;
        }

        public void Accept(IMapLiteralVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
