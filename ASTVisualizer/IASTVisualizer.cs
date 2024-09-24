using System;
using System.Collections.Generic;
using AST.Nodes;

namespace ASTVisualizer
{
    public interface IASTVisualizer
    {
        /// <summary>
        /// Generates a mermaid diagram from the given node as a string.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public string Visualize(INode node);
    }
}
