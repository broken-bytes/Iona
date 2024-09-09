using AST.Nodes;

namespace AST
{
    internal static class Utils
    {
        public static INode GetRoot(INode node)
        {
            while (node.Parent != null)
            {
                node = node.Parent;
            }

            return node;
        }
    }
}
