using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AST.Nodes
{
    public interface IExpressionNode
    {
        public ExpressionType ExpressionType { get; set; }
        public ITypeNode ResultType { get; set; }
    }
}
