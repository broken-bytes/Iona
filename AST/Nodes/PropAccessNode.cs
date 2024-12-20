﻿using System.Text;
using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class PropAccessNode : IExpressionNode
    {
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public IExpressionNode Object { get; set; }
        public IExpressionNode Property { get; set; }
        public TypeReferenceNode? ResultType { get; set; }
        public ExpressionType ExpressionType => ExpressionType.PropAccess;
        public FileNode Root => Utils.GetRoot(this);
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public PropAccessNode(IExpressionNode obj, IExpressionNode property, INode? parent = null)
        {
            Object = obj;
            Property = property;
            Parent = parent;
            Type = NodeType.PropAccess;
            Meta = property.Meta;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (Object is SelfNode selfNode)
            {
                builder.Append(selfNode);
            }
            else
            {
                builder.Append(Object);
            }
            
            builder.Append('.');

            if (Property is IdentifierNode identifierNode)
            {
                builder.Append(identifierNode);
            }
            else
            {
                builder.Append(Property);
            }
            
            return builder.ToString();
        }

        public void Accept(IPropAccessVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
