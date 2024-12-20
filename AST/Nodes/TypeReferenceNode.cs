﻿using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class TypeReferenceNode : ITypeReferenceNode
    {
        public string FullyQualifiedName { get; set; }
        public string Name { get; set; }
        public string Assembly { get; set; }
        public Kind TypeKind { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }
        public TypeReferenceKind ReferenceKind { get; set; }
        public List<ITypeReferenceNode> GenericArguments { get; set; } = new List<ITypeReferenceNode>();

        public TypeReferenceNode(string name, INode? parent = null)
        {
            // Temporarily set the name to the FQN
            FullyQualifiedName = name;
            Name = name;
            TypeKind = Kind.Unknown;
            Parent = parent;
            Type = NodeType.TypeReference;
            ReferenceKind = TypeReferenceKind.Concrete;
        }

        public void Accept(ITypeReferenceVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
