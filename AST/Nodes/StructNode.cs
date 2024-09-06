using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class StructNode : IAccessLevelNode, IStatementNode, ITypeNode
    {
        public string Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public AccessLevel AccessLevel { get; set; }
        public StatementType StatementType { get; set; }
        public List<IdentifierNode> Contracts { get; set; }
        public List<GenericArgument> GenericArguments { get; set; } = new List<GenericArgument>();
        public BlockNode? Body { get; set; }

        public StructNode(string name, AccessLevel accessLevel, List<IdentifierNode> contracts, INode? parent)
        {
            Name = name;
            Parent = parent;
            Type = NodeType.Declaration;
            AccessLevel = accessLevel;
            Contracts = contracts;
            StatementType = StatementType.ClassDeclaration;
        }

        public void Accept(IStructVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
