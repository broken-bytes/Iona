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

        public static ModuleNode GetModule(INode node)
        {
            while (node.Parent != null)
            {
                if (node is ModuleNode module)
                {
                    return module;
                }

                node = node.Parent;
            }

            return new ModuleNode("Global", "Global", null);
        }
    }
}
