using AST.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AST.Nodes
{
    public interface ITypeNode : INode
    {
        public NodeKind Kind { get; set; }
    }
}
