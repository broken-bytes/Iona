using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class EnumNode : IAccessLevelNode, IStatementNode
    {
        public string Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public AccessLevel AccessLevel { get; set; }
        public StatementType StatementType { get; set; }
        public List<IdentifierNode> Contracts { get; set; }
        public BlockNode? Body { get; set; }

        public EnumNode(string name, AccessLevel accessLevel, List<IdentifierNode> contracts, INode? parent)
        {
            Name = name;
            Parent = parent;
            Type = NodeType.Declaration;
            AccessLevel = accessLevel;
            Contracts = contracts;
            StatementType = StatementType.EnumDeclaration;
        }

        public void Accept(IEnumVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
