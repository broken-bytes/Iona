namespace IR.Instructions;

/// <summary>
/// Unconditional branch to a basic block. A terminator instruction.
/// LLVM-style: br label %target.
/// </summary>
public class BranchInst : IrInstruction
{
    /// <summary>
    /// The target basic block to jump to.
    /// </summary>
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
