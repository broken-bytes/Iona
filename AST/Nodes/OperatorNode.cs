﻿using AST.Types;
using AST.Visitors;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class OperatorNode : IAccessLevelNode, IStatementNode
    {
        public OperatorType Op { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public AccessLevel AccessLevel { get; set; }
        public StatementType StatementType { get; set; }
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();
        public INode? ReturnType { get; set; }
        public bool IsStatic { get; set; }
        public BlockNode? Body { get; set; }
        public Metadata Meta { get; set; }

        public OperatorNode(
            OperatorType op,
            AccessLevel access,
            bool isStatic,
            INode? parent = null
        )
        {
            Op = op;
            Parent = parent;
            Type = NodeType.Declaration;
            StatementType = StatementType.OperatorDeclaration;
            AccessLevel = access;
            IsStatic = isStatic;
        }

        public void Accept(IOperatorVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}