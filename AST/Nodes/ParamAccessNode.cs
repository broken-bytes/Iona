using System.Reflection.Metadata.Ecma335;
using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class ParamAccessNode : IExpressionNode
    {
        public IdentifierNode Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public ExpressionType ExpressionType => ExpressionType.Identifier;
        public TypeReferenceNode? ResultType { get; set; }
        public FileNode Root { get => Utils.GetRoot(this); }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public ParamAccessNode(IdentifierNode name, INode? parent = null)
        {
            Name = name;
            Parent = parent;
            Type = NodeType.ParameterAccess;
        }

        public void Accept(IParamAccessVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}