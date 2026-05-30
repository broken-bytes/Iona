//|--- FunctionTypeNode.cs -------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Types;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    // `(T1, T2) -> R` — a structural function type. Inherits TypeReferenceNode so it slots
    // into every place that already takes a type ref; the resolver fills in FullyQualifiedName
    // (`System.Func<T1,T2,R>` or `System.Action<T1,T2>` for `R = Void`).
    public class FunctionTypeNode : TypeReferenceNode
    {
        public List<ITypeReferenceNode> ParameterTypes { get; set; } = new();
        public ITypeReferenceNode? ReturnType { get; set; }

        public FunctionTypeNode(INode? parent = null) : base("System.Func", parent)
        {
            Type = NodeType.FunctionType;
            TypeKind = Kind.Class;
            Status = ResolutionStatus.Resolved;
        }
    }
}
