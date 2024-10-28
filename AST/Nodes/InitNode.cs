using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

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
        public List<ParameterNode> Parameters { get; set; } = new List<ParameterNode>();
        public BlockNode? Body { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

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
