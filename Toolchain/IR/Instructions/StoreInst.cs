//|--- StoreInst.cs --------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class StoreInst : IrInstruction
{
    public IrValue Value { get; }

    public IrValue Destination { get; }

    public StoreInst(IrValue value, IrValue destination)
    {
        Value = value;
        Destination = destination;
        // Store does not produce a result value.
    }

    public override string Print()
    {
        return $"store {Value} -> {Destination} : {Value.Type}";
    }
}
