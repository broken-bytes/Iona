using AST.Types;
using AST.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class ImportNode : INode
    {
        public string Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public StatementType StatementType { get; set; }
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
