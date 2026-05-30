//|--- AttributeNode.cs ----------------------------------------|
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
    public class AttributeNode : INode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public string Name { get; set; }
        public List<FuncCallArg> Args { get; set; } = [];
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public AttributeNode(string name, INode? parent = null)
        {
            Type = NodeType.Statement;
            Parent = parent;
            Name = name;
        }
    }
}
