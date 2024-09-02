﻿using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class VariableNode : IStatementNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public AccessLevel AccessLevel { get; set; }
        public StatementType StatementType { get; set; }
        public Type? VariableType { get; set; }
        public IExpressionNode? Value { get; set; }

        public VariableNode(string name, INode? parent)
        {
            Name = name;
            Module = "";
            Value = null;
            Parent = parent;
            Type = NodeType.Declaration;
            VariableType = null;
            StatementType = StatementType.VariableDeclaration;
        }

        public void Accept(IVariableVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
