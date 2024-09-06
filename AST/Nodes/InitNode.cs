using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class InitNode : IAccessLevelNode, IStatementNode
    {
        public string Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public AccessLevel AccessLevel { get; set; }
        public StatementType StatementType { get; set; }
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();
        public BlockNode? Body { get; set; }

        public InitNode(
            AccessLevel access,
            INode? parent = null
        )
        {
            Name = "init";
            Parent = parent;
            Type = NodeType.Declaration;
            StatementType = StatementType.InitDeclaration;
            AccessLevel = access;
        }

        public void Accept(IInitVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
