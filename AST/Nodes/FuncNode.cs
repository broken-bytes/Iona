using AST.Types;
using AST.Visitors;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class FuncNode : IAccessLevelNode, IStatementNode
    {
        public string Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public AccessLevel AccessLevel { get; set; }
        public StatementType StatementType { get; set; }
        public List<ParameterNode> Parameters { get; set; } = new List<ParameterNode>();
        public INode? ReturnType { get; set; }
        public bool IsMutable { get; set; }
        public bool IsStatic { get; set; }
        public BlockNode? Body { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public FuncNode(
            string name,
            AccessLevel access,
            bool isMutable,
            bool isStatic,
            INode? parent = null
        )
        {
            Name = name;
            Parent = parent;
            Type = NodeType.Declaration;
            StatementType = StatementType.FunctionDeclaration;
            AccessLevel = access;
            IsMutable = isMutable;
            IsStatic = isStatic;
        }

        public void Accept(IFuncVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
