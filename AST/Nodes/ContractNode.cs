﻿using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class ContractNode : IAccessLevelNode, IStatementNode, ITypeNode
    {
        public string FullyQualifiedName { get; set; }
        public string Name { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public AccessLevel AccessLevel { get; set; }
        public StatementType StatementType { get; set; }
        public List<INode> Refinements { get; set; } = new List<INode>();
        public FileNode Root => Utils.GetRoot(this);
        public List<GenericArgument> GenericArguments { get; set; } = new List<GenericArgument>();
        public BlockNode? Body { get; set; }
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public ContractNode(string name, AccessLevel access, INode? parent = null)
        {
            Name = name;
            Parent = parent;
            Type = NodeType.Declaration;
            StatementType = StatementType.ContractDeclaration;
            AccessLevel = access;
        }

        public void Accept(IContractVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
