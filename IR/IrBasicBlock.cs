namespace IR;

/// <summary>
/// A basic block: a straight-line sequence of instructions with a single entry
/// point and a single exit (the terminator). LLVM-style basic blocks form the
/// nodes of the control flow graph.
/// </summary>
public class IrBasicBlock
{
    /// <summary>
    /// The label for this block (e.g. "entry", "then", "else", "merge").
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// The ordered list of instructions in this block.
    /// The last instruction must be a terminator.
    /// </summary>
    public List<IrInstruction> Instructions { get; } = new();

    public IrBasicBlock(string label)
    {
        Label = label;
    }

    /// <summary>
    /// Appends an instruction to the end of this block.
    /// </summary>
    public void Append(IrInstruction instruction)
    {
        Instructions.Add(instruction);
    }

    /// <summary>
    /// Returns the terminator instruction of this block, or null if the block
    /// is not yet terminated.
    /// </summary>
    public IrInstruction? Terminator =>
        Instructions.Count > 0 && Instructions[^1].IsTerminator
            ? Instructions[^1]
            : null;

    /// <summary>
    /// Whether this block has a terminator instruction.
    /// </summary>
    public bool IsTerminated => Terminator != null;
}
