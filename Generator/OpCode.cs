using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generator
{
    internal enum OpCode
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Mod,
        Box,
        Castclass,
        Unbox_Any,
        Call,
        Newobj,
        LoadArgument,
        LoadDouble,
        LoadFalse,
        LoadField,
        LoadFloat,
        LoadInt,
        LoadString,
        LoadTrue,
        LoadVariable,
        SetField,
        SetVariable,
        Return,
        Void
    }
}
