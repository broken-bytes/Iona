using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AST.Types
{
    public struct GenericConstraint
    {
        public GenericCondition Condition { get; set; }
        public IType? Type { get; set; }
    }
}
