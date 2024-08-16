using AST.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
