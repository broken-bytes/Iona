using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AST.Types
{
    public enum StatementType
    {
        Import,
        VariableDeclaration,
        VariableAssignment,
        Return,
        If,
        While,
        For,
        Break,
        Continue,
    }
}
