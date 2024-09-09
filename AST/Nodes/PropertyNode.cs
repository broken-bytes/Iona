using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class PropertyNode : IAccessLevelNode, IStatementNode
    {
        public string Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public AccessLevel AccessLevel { get; set; }
        bool IsMutable { get; set; }
        public StatementType StatementType { get; set; }
        public IType? TypeNode { get; set; }
        public IExpressionNode? Value { get; set; }

        public PropertyNode(
            string name,
            AccessLevel accessLevel,
            bool isMutable,
            IType? type = null,
            IExpressionNode? value = null,
            INode? parent = null
        )
        {
            Name = name;
            AccessLevel = accessLevel;
            Value = value;
            IsMutable = isMutable;
            Parent = parent;
            Type = NodeType.Declaration;
            TypeNode = type;
            StatementType = StatementType.PropertyDeclaration;
        }

        public void Accept(IPropertyVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
