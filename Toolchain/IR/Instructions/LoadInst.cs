//|--- LoadInst.cs ---------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class LoadInst : IrInstruction
{
    public IrValue Source { get; }

    public LoadInst(IrValue source, IrValue result)
    {
        Source = source;
        Result = result;
    }

    public override string Print()
    {
        return $"{Result} = load {Source} : {Result!.Type}";
    }
}
