using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class MemberAccessExpressionNode : INode
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public INode Parent { get; set; }
        public NodeType Type { get; set; }
        public INode Root => Utils.GetRoot(this);
        public INode Target { get; set; }
        public INode Member { get; set; }

        public MemberAccessExpressionNode(string name, string module, INode target, INode member, INode parent)
        {
            Name = name;
            Module = module;
            Parent = parent;
            Type = NodeType.MemberAccess;
            Target = target;
            Member = member;
        }

        public void Accept(IMemberAccessVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
