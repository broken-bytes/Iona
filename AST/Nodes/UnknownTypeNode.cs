﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AST.Types;

namespace AST.Nodes
{
    internal class UnknownTypeNode : ITypeNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public INode Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public NodeKind Kind { get; set; }

        public UnknownTypeNode(string name, string module, INode parent)
        {
            Name = name;
            Module = module;
            Parent = parent;
            Type = NodeType.NominalType;
            Kind = NodeKind.UnknownKind;
        }
    }
}
