//|--- AddInst.cs ----------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class AddInst : BinaryInst
{
    public AddInst(IrValue left, IrValue right, IrValue result)
        : base(BinaryOp.Add, left, right, result) { }

    public override string Print()
    {
        return $"{Result} = add {Result!.Type} {Left}, {Right}";
    }
}
