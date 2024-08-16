using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AST.Nodes;

namespace AST.Types
{
    public struct Parameter
    {
        public string Name;
        public ITypeNode Type;
    }
}
