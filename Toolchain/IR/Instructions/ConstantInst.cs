//|--- ConstantInst.cs -----------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class ConstantInst : IrInstruction
{
    public ConstantKind ConstantKind { get; }

    public string RawValue { get; }

    public IrType ConstantType { get; }

    public ConstantInst(ConstantKind kind, string rawValue, IrType type, IrValue result)
    {
        ConstantKind = kind;
        RawValue = rawValue;
        ConstantType = type;
        Result = result;
    }

    public override string Print()
    {
        var display = ConstantKind == ConstantKind.String ? $"\"{RawValue}\"" : RawValue;
        return $"{Result} = constant {ConstantKind.ToString().ToLowerInvariant()} {display} : {ConstantType}";
    }
}

public enum ConstantKind
{
    Integer,
    Float,
    Double,
    Bool,
    String,
    Char,
    Null
}
