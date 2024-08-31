using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class LiteralNode: IExpressionNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public INode Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public ExpressionType ExpressionType => ExpressionType.Literal;
        public ITypeNode? ResultType { get; set; }
        public LiteralType LiteralType { get; set; }
        public string Value { get; set; }

        public LiteralNode(string name, string module, INode parent, LiteralType literalType, string value)
        {
            Name = name;
            Module = module;
            Parent = parent;
            Type = NodeType.Literal;
            LiteralType = literalType;
            Value = value;
        }

        public void Accept(ILiteralVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
