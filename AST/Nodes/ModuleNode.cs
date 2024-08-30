using AST.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AST.Nodes
{
    public class ModuleNode : INode, IStatementNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public INode Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public StatementType StatementType { get; set; }

        public ModuleNode(string name, INode parent)
        {
            Name = name;
            Module = "";
            Parent = parent;
            Type = NodeType.Declaration;
            StatementType = StatementType.ModuleDeclaration;
        }
    }
}
