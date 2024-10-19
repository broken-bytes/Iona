using AST.Types;
using AST.Visitors;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class ClassNode : IAccessLevelNode, IStatementNode, ITypeNode
    {
        public string FullyQualifiedName { get; set; }
        public string Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public AccessLevel AccessLevel { get; set; }
        public StatementType StatementType { get; set; }
        public List<INode> Contracts { get; set; } = new List<INode>();
        public List<GenericArgument> GenericArguments { get; set; } = new List<GenericArgument>();
        public BlockNode? Body { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public ClassNode(string name, AccessLevel accessLevel, INode? parent = null)
        {
            Name = name;
            Parent = parent;
            Type = NodeType.Declaration;
            AccessLevel = accessLevel;
            StatementType = StatementType.ClassDeclaration;
        }

        public void Accept(IClassVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
