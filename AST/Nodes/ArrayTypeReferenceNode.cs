using AST.Types;

namespace AST.Nodes
{
    public class ArrayTypeReferenceNode : INode
    {
        public INode ElementType { get; set; }
        public INode? Parent { get; set; }
        public NodeType Type { get; set; }

        public INode Root => throw new NotImplementedException();

        public ArrayTypeReferenceNode(INode element)
        {
            ElementType = element;
        }
    }
}
