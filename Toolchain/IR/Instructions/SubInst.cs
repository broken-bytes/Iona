//|--- SubInst.cs ----------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class SubInst : BinaryInst
{
    public SubInst(IrValue left, IrValue right, IrValue result)
        : base(BinaryOp.Sub, left, right, result) { }

    public override string Print()
    {
        return $"{Result} = sub {Result!.Type} {Left}, {Right}";
    }
}
