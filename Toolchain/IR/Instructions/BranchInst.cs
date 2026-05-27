//|--- BranchInst.cs -------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class BranchInst : IrInstruction
{
    public IrBasicBlock Target { get; }

    public override bool IsTerminator => true;

    public BranchInst(IrBasicBlock target)
    {
        Target = target;
    }

    public override string Print()
    {
        return $"br label %{Target.Label}";
    }
}
