using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AST.Types;

namespace AST.Nodes
{
    public interface INode
    {
        string Name { get; set; }
        string Module { get; set; }
        INode Parent { get; set; }
        NodeType Type { get; set; }
        INode Root { get; }
    }
}
