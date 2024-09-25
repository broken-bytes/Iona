using AST.Types;
using AST.Visitors;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class VariableNode : IStatementNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public AccessLevel AccessLevel { get; set; }
        public StatementType StatementType { get; set; }
        public INode? TypeNode { get; set; }
        public INode? Value { get; set; }
        bool IsAssigned { get; set; }
        public Metadata Meta { get; set; }

        public VariableNode(string name, INode? value, INode? parent = null)
        {
            Name = name;
            Module = "";
            Value = value;
            Parent = parent;
            Type = NodeType.Declaration;
            StatementType = StatementType.VariableDeclaration;
            IsAssigned = value != null;
        }

        public void Accept(IVariableVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
