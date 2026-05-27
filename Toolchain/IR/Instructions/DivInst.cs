//|--- DivInst.cs ----------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class DivInst : BinaryInst
{
    public DivInst(IrValue left, IrValue right, IrValue result)
        : base(BinaryOp.Div, left, right, result) { }

    public override string Print()
    {
        return $"{Result} = div {Result!.Type} {Left}, {Right}";
    }
}
