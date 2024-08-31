using AST.Types;
using AST.Visitors;

namespace AST.Nodes
{
    public class IdentifierNode
    {
        public string Name { get; set; }
        public string Module { get; set; }
         public INode? Parent { get; set; }
        public NodeType Type { get; set; }

        public IdentifierNode(string name, string module, INode? parent)
        {
            Name = name;
            Module = module;
            Parent = parent;
            Type = NodeType.Identifier;
        }

        public void Accept(IIdentifierVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
