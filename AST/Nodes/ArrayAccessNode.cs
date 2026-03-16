using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class ArrayAccessNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public IExpressionNode Array { get; set; }
        public IExpressionNode Index { get; set; }
        public TypeReferenceNode? ResultType { get; set; }
        public ExpressionType ExpressionType => ExpressionType.ArrayAccess;
        public FileNode Root => Utils.GetRoot(this);
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public ArrayAccessNode(IExpressionNode array, IExpressionNode index, INode? parent = null)
        {
            Array = array;
            Index = index;
            Parent = parent;
            Type = NodeType.ArrayAccess;
            Meta = array.Meta;
        }

        public override string ToString()
        {
            return $"{Array}[{Index}]";
        }

        public void Accept(IArrayAccessVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
