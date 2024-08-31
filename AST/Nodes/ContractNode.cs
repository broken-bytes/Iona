using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class ContractNode : IAccessLevelNode, IStatementNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
         public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public AccessLevel AccessLevel { get; set; }
        public StatementType StatementType { get; set; }
        public List<IdentifierNode> Refinements { get; set; } = new List<IdentifierNode>();
        public INode Root => Utils.GetRoot(this);
        public BlockNode? Body { get; set; }

        public ContractNode(string name, string module, AccessLevel access, INode? parent)
        {
            Name = name;
            Module = module;
            Parent = parent;
            Type = NodeType.Declaration;
            StatementType = StatementType.ContractDeclaration;
            AccessLevel = access;
        }

        public void Accept(IContractVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
