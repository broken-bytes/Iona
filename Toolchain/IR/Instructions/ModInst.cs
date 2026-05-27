//|--- ModInst.cs ----------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class ModInst : BinaryInst
{
    public ModInst(IrValue left, IrValue right, IrValue result)
        : base(BinaryOp.Mod, left, right, result) { }

    public override string Print()
    {
        return $"{Result} = mod {Result!.Type} {Left}, {Right}";
    }
}
