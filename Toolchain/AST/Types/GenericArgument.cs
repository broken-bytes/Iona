//|--- GenericArgument.cs --------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Nodes;
using Shared;
using static AST.Nodes.INode;

namespace AST.Types
{
    public class GenericArgument : INode
    {
        public string Name { get; set; }
        // Constraints declared via `#over<T: Numeric & Comparable>` or `where T: A & B`.
        // Each entry is one bound; the type must satisfy them all (intersection / logical AND).
        public List<TypeReferenceNode> Constraints { get; set; } = new();
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public GenericArgument(string name, INode? parent = null)
        {
            Name = name;
            Parent = parent;
            Type = NodeType.GenericType;
        }
    }
}
