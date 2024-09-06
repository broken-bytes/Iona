using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AST.Types
{
    public struct GenericArgument
    {
        public string Name { get; set; }
        public List<GenericConstraint> Constraints { get; set; }
    }
}
