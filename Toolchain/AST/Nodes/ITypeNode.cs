//|--- ITypeNode.cs --------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Types;

namespace AST.Nodes
{
    public interface ITypeNode : INode
    {
        public string FullyQualifiedName { get; set; }
        public string Name { get; set; }
        public List<GenericArgument> GenericArguments { get; set; }
        public bool IsGeneric => GenericArguments.Count > 0;
        public BlockNode? Body { get; set; }
    }
}
