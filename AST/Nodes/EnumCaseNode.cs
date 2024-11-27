using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class EnumCaseNode : IStatementNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public StatementType StatementType { get; set; }
        public IExpressionNode? Value { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public EnumCaseNode(string name, IExpressionNode? value, INode? parent = null)
        {
            Name = name;
            Module = "";
            Value = value;
            Parent = parent;
            Type = NodeType.Declaration;
            StatementType = StatementType.VariableDeclaration;
        }

        public void Accept(IEnumCaseVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}