//|--- PhiInst.cs ----------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class PhiInst : IrInstruction
{
    public List<PhiIncoming> Incomings { get; } = new();

    public PhiInst(IrValue result)
    {
        Result = result;
    }

    public void AddIncoming(IrValue value, IrBasicBlock block)
    {
        Incomings.Add(new PhiIncoming(value, block));
    }

    public override string Print()
    {
        var pairs = string.Join(", ", Incomings.Select(i => $"[ {i.Value}, %{i.Block.Label} ]"));
        return $"{Result} = phi {Result!.Type} {pairs}";
    }
}

public class PhiIncoming
{
    public IrValue Value { get; }
    public IrBasicBlock Block { get; }

    public PhiIncoming(IrValue value, IrBasicBlock block)
    {
        Value = value;
        Block = block;
    }
}
