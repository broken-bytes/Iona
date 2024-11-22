using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class LiteralNode : IExpressionNode
    {
        public string Value { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public ExpressionType ExpressionType => ExpressionType.Literal;
        public TypeReferenceNode? ResultType { get; set; }
        public LiteralType LiteralType { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public LiteralNode(string value, LiteralType literalType, INode? parent = null)
        {
            Value = value;
            Parent = parent;
            Type = NodeType.Literal;
            LiteralType = literalType;
            Value = value;
        }

        public override string ToString()
        {
            switch (LiteralType)
            {
                case LiteralType.Float:
                    return $"{Value}f";
                default:
                    return Value;
            }
        }

        public void Accept(ILiteralVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
