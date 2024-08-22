using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AST.Types;

namespace AST.Nodes
{
    public interface IStatementNode : INode
    {
        public StatementType StatementType { get; set; }
    }
}
