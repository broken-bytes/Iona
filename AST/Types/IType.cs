using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AST.Types
{
    public interface IType
    {
        public string Name { get; set; }
        public string Module { get; set; }
        public Kind TypeKind { get; set; }
    }
}
