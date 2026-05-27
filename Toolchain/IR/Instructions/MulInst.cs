//|--- MulInst.cs ----------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class MulInst : BinaryInst
{
    public MulInst(IrValue left, IrValue right, IrValue result)
        : base(BinaryOp.Mul, left, right, result) { }

    public override string Print()
    {
        return $"{Result} = mul {Result!.Type} {Left}, {Right}";
    }
}
