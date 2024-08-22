using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AST.Types
{
    public enum NodeType
    {
        Allocation,
        Assignment,
        Contract,
        Deallocation,
        Expression,
        Statement,
        NominalType,
        Import,
        Init,
        Func,
        FuncCall,
        PropertyDecl,
        VariableDecl,
        Literal,
        MemberAccess,
        Identifier,
        Operator,
        IfStatement,
        ElseStatement,
        ForLoop,
        WhileLoop,
        BreakStatement,
        ContinueStatement,
        ReturnStatement,
        TryBlock,
        CatchBlock,
        File,
        Module,
        Namespace,
        Annotation,
        Program,
        CodeBlock,
    }
}
