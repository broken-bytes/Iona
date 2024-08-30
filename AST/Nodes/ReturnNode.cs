using AST.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AST.Nodes
{
    public class ReturnNode : INode, IStatementNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public INode Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public StatementType StatementType { get; set; }
        public IExpressionNode Value { get; set; }

        public ReturnNode(string name, IExpressionNode value, INode parent)
        {
            Name = name;
            Module = "";
            Value = value;
            Parent = parent;
            Type = NodeType.Statement;
            StatementType = StatementType.ReturnStatement;
        }
    }
}
