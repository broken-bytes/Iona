//|--- IrBasicBlock.cs -----------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR;

public class IrBasicBlock
{
    public string Label { get; }

    public List<IrInstruction> Instructions { get; } = new();

    public IrBasicBlock(string label)
    {
        Label = label;
    }

    public void Append(IrInstruction instruction)
    {
        Instructions.Add(instruction);
    }

    public IrInstruction? Terminator =>
        Instructions.Count > 0 && Instructions[^1].IsTerminator
            ? Instructions[^1]
            : null;

    public bool IsTerminated => Terminator != null;
}
