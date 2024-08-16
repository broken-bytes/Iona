using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AST.Types
{
    public enum ExpressionType
    {
        Literal,
        Identifier,
        FunctionCall,
        BinaryOperation,
        UnaryOperation,
        TypeCast,
        TypeOf,
        SizeOf,
        ArrayAccess,
        MemberAccess,
        ObjectLiteral,
        PointerAccess,
        PointerDereference,
        AddressOf,
        New,
        Delete,
        Lambda,
        Tuple,
        Array,
        Struct,
        Union,
        Enum,
        If,
        While,
        For,
        Switch,
        Return,
        Break,
        Continue,
        Label,
        Block,
        Parentheses,
        Error,
        Noop,
    }
}
