using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AST.Nodes;

namespace AST.Types
{
    public struct FuncCallArg
    {
        public string Name;
        public IExpressionNode Value;
    }
}
