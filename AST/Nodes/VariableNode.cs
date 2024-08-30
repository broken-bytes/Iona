using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AST.Types;

namespace AST.Nodes
{
    public class VariableNode : IStatementNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public INode Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public AccessLevel AccessLevel { get; set; }
        public StatementType StatementType { get; set; }
        public ITypeNode TypeNode { get; set; }
        public IExpressionNode Value { get; set; }

        public VariableNode(string name, ITypeNode type, IExpressionNode value, INode parent)
        {
            Name = name;
            Module = "";
            Value = value;
            Parent = parent;
            Type = NodeType.Declaration;
            TypeNode = type;
            StatementType = StatementType.VariableDeclaration;
        }
    }
}
