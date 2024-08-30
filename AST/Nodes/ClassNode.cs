using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AST.Types;

namespace AST.Nodes
{
    public class ClassNode : IAccessLevelNode, IStatementNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public INode Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public AccessLevel AccessLevel { get; set; }
        public StatementType StatementType { get; set; }
        public List<IdentifierNode> Contracts { get; set; }
        public BlockNode? Body { get; set; }

        public ClassNode(string name, string module, AccessLevel accessLevel, List<IdentifierNode> contracts, INode parent)
        {
            Name = name;
            Module = module;
            Parent = parent;
            Type = NodeType.Declaration;
            AccessLevel = accessLevel;
            Contracts = contracts;
            StatementType = StatementType.ClassDeclaration;
        }
    }
}
