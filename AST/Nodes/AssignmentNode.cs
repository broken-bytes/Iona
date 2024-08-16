using AST.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AST.Nodes
{
    public class AssignmentNode : INode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public INode Parent { get; set; }
        public NodeType Type { get; set; }
        public NodeKind Kind { get; set; }
        public AssignmentType AssignmentType { get; set; }
        public INode Root { get => Utils.GetRoot(this); }

        public AssignmentNode(
            string name,
            string module,
            AssignmentType assignmentType,
            INode parent
        )
        {
            Name = name;
            Module = module;
            Parent = parent;
            Type = NodeType.Assignment;
            AssignmentType = assignmentType;
        }
    }
}
