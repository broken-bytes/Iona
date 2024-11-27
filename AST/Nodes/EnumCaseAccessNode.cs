using System.Text;
using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class EnumCaseAccessNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public IdentifierNode Case { get; set; }
        public TypeReferenceNode? ResultType { get; set; }
        public ExpressionType ExpressionType => ExpressionType.EnumCaseAcces;
        public FileNode Root => Utils.GetRoot(this);
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public EnumCaseAccessNode(IdentifierNode @case, INode? parent = null)
        {
            Case = @case;
            Parent = parent;
            Type = NodeType.EnumCaseAccess;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            
            builder.AppendLine($"{Case}");
            
            return builder.ToString();
        }

        public void Accept(IEnumCaseAccessVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}