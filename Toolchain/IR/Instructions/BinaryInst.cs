//|--- BinaryInst.cs -------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class BinaryInst : IrInstruction
{
    public BinaryOp Op { get; }
    public IrValue Left { get; }
    public IrValue Right { get; }

    public BinaryInst(BinaryOp op, IrValue left, IrValue right, IrValue result)
    {
        Op = op;
        Left = left;
        Right = right;
        Result = result;
    }

    public override string Print()
    {
        var opName = Op.ToString().ToLowerInvariant();
        return $"{Result} = binary {opName} {Left}, {Right} : {Result!.Type}";
    }
}

public enum BinaryOp
{
    Add,
    Sub,
    Mul,
    Div,
    Mod,
    And,
    Or
}
