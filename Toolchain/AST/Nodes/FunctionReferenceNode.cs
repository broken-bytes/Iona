//|--- FunctionReferenceNode.cs --------------------------------|
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
    // `::name` — a reference to a free function or a static method, taken as a value.
    // Lowered by codegen to `newobj Func<...>(<target>, ldftn <method>)`.
    public class FunctionReferenceNode : IExpressionNode
    {
        public string Name { get; set; }
        public string? Scope { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public ExpressionType ExpressionType => ExpressionType.Literal;
        public TypeReferenceNode? ResultType { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public FunctionReferenceNode(string name, string? scope = null, INode? parent = null)
        {
            Name = name;
            Scope = scope;
            Parent = parent;
            Type = NodeType.FunctionReference;
        }

        public override string ToString()
        {
            return Scope == null ? $"::{Name}" : $"::{Scope}.{Name}";
        }

        public void Accept(IFunctionReferenceVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
