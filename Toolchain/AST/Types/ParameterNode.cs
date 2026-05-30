//|--- ParameterNode.cs ----------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Nodes;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Types
{
    public class ParameterNode : INode
    {
        public string Name { get; set; }
        public TypeReferenceNode TypeNode { get; set; }
        public List<AttributeNode> Attributes { get; set; } = new();
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public ResolutionStatus Status { get; set; }
        public Metadata Meta { get; set; }

        public ParameterNode(string name, TypeReferenceNode typeNode, INode? parent = null)
        {
            Name = name;
            TypeNode = typeNode;
            Parent = parent;
            Type = NodeType.Parameter;
            Status = ResolutionStatus.Unresolved;
        }

        public override string ToString()
        {
            return Name;
        }

        public void Accept(IParameterVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
