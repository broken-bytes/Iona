using AST.Types;
using Shared;

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

            nodeOrder.Add(this);

            return nodeOrder;
        }
    }
}
