using AST.Types;
using AST.Visitors;
using Shared;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class ModuleNode : IStatementNode
    {
        public string Name { get; set; }
        public string Assembly { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public FileNode Root => Utils.GetRoot(this);
        public StatementType StatementType { get; set; }
        public List<INode> Children { get; private set; } = new List<INode>();
        public ResolutionStatus Status { get; set; } = ResolutionStatus.Unresolved;
        public Metadata Meta { get; set; }

        public ModuleNode(string name, string assembly, INode? parent)
        {
            Name = name;
            Assembly = assembly;
            Parent = parent;
            Type = NodeType.Declaration;
            StatementType = StatementType.ModuleDeclaration;
        }

        public void AddChild(INode child)
        {
            Children.Add(child);
            child.Parent = this;
        }

        public void Accept(IModuleVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
