using AST.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AST.Nodes
{
    public class FuncNode : IAccessLevelNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public INode Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public AccessLevel AccessLevel { get; set; }
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();
        public ITypeNode? ReturnType { get; set; }
        bool IsMutable { get; set; }
        public bool IsStatic { get; set; }
        public BlockNode? Body { get; set; }

        public FuncNode(
            string name, 
            string module, 
            AccessLevel access, 
            bool isMutable,
            bool isStatic, 
            INode parent
        )
        {
            Name = name;
            Module = module;
            Parent = parent;
            Type = NodeType.Func;
            AccessLevel = access;
            IsMutable = isMutable;
            IsStatic = isStatic;
        }
    }
}
