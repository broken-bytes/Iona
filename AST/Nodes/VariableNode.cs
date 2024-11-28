using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class VariableNode : IStatementNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public AccessLevel AccessLevel { get; set; }
        public StatementType StatementType { get; set; }
        public TypeReferenceNode? TypeNode { get; set; }
        public IExpressionNode? Value { get; set; }
        bool IsAssigned { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public VariableNode(string name, IExpressionNode? value, INode? parent = null)
        {
            Name = name;
            Module = "";
            Value = value;
            Parent = parent;
            Type = NodeType.Declaration;
            StatementType = StatementType.VariableDeclaration;
            IsAssigned = value != null;
        }

        public override string ToString()
        {
            return Name;
        }

        public void Accept(IVariableVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
