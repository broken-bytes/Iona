using AST.Types;
using static AST.Nodes.INode;

namespace AST.Nodes
{
    public class ArrayTypeReferenceNode : INode
    {
        public INode ElementType { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }
        public Metadata Meta { get; set; }

        public INode Root => throw new NotImplementedException();

        public ArrayTypeReferenceNode(INode element)
        {
            ElementType = element;
        }
    }
}
