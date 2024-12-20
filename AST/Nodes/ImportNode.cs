﻿using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class ImportNode : INode
    {
        public string Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public StatementType StatementType { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public ImportNode(string name, INode? parent)
        {
            Name = name;
            Parent = parent;
            Type = NodeType.Import;
            StatementType = StatementType.Import;
        }

        public void Accept(IImportVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
