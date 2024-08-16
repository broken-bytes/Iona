using AST.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AST.Nodes
{
    public class ContractNode : IAccessLevelNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public INode Parent { get; set; }
        public NodeType Type { get; set; }
        public AccessLevel AccessLevel { get; set; }
        public List<IdentifierNode> Refinements { get; set; } = new List<IdentifierNode>();
        public INode Root => Utils.GetRoot(this);

        public ContractNode(string name, string module, AccessLevel access, INode parent)
        {
            Name = name;
            Module = module;
            Parent = parent;
            Type = NodeType.Contract;
            AccessLevel = access;
        }
    }
}
