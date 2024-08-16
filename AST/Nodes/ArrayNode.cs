using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AST.Types;

namespace AST.Nodes
{
    public class ArrayNode : ITypeNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public INode Parent { get; set; }
        public NodeType Type { get; set; }
        public NodeKind Kind { get; set; }
        public ITypeNode ItemType { get; set; }
        public INode Root { get => Utils.GetRoot(this); }

        public ArrayNode(
            string name, 
            string module, 
            INode parent, 
            NodeType type, 
            NodeKind kind,
            ITypeNode itemType
        )
        {
            Name = name;
            Module = module;
            Parent = parent;
            Type = type;
            Kind = kind;
            ItemType = itemType;
        }
    }
}
