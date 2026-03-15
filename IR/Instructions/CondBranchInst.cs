namespace IR.Instructions;

/// <summary>
/// Conditional branch. A terminator instruction.
/// LLVM-style: br i1 %cond, label %then, label %else.
/// </summary>
public class CondBranchInst : IrInstruction
{
    /// <summary>
    /// The condition value (must be bool).
    /// </summary>
    public IrValue Condition { get; }

    /// <summary>
    /// The block to branch to if the condition is true.
    /// </summary>
    public IrBasicBlock ThenBlock { get; }

    /// <summary>
    /// The block to branch to if the condition is false.
    /// </summary>
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
