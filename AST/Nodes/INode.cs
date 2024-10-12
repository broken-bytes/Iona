using AST.Types;

namespace AST.Nodes
{
    public interface INode
    {
        public enum ResolutionStatus
        {
            Unresolved,
            Resolving,
            Resolved,
            Failed
        }

        public struct Metadata
        {
            public string File { get; set; }
            public int LineStart { get; set; }
            public int LineEnd { get; set; }
            public int ColumnStart { get; set; }
            public int ColumnEnd { get; set; }
        }

        INode? Parent { get; set; }
        NodeType Type { get; set; }
        INode Root { get; }
        ResolutionStatus Status { get; set; }
        Metadata Meta { get; set; }

        public List<INode> Hierarchy()
        {
            List<INode> nodeOrder = new List<INode>();
            INode current = this;

            while (current.Parent != null)
            {
                current = current.Parent;
                nodeOrder.Add(current);
            }

            nodeOrder.Reverse();

            // The deepest node is the node itself so it is added last
            nodeOrder.Add(this);

            return nodeOrder;
        }
    }
}
