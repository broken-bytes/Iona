//|--- CondBranchInst.cs ---------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class CondBranchInst : IrInstruction
{
    public IrValue Condition { get; }

    public IrBasicBlock ThenBlock { get; }

    public IrBasicBlock ElseBlock { get; }

    public override bool IsTerminator => true;

    public CondBranchInst(IrValue condition, IrBasicBlock thenBlock, IrBasicBlock elseBlock)
    {
        Condition = condition;
        ThenBlock = thenBlock;
        ElseBlock = elseBlock;
    }

    public override string Print()
    {
        return $"cond_br {Condition}, label %{ThenBlock.Label}, label %{ElseBlock.Label}";
    }
}
