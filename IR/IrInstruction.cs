namespace IR;

/// <summary>
/// Base class for all IR instructions. Each instruction may optionally produce
/// a result value (SSA register). Instructions that are terminators (branch, return)
/// end a basic block.
///
/// LLVM-inspired: flat instruction set, each instruction is self-contained.
/// SIL-inspired: instructions carry full type information.
/// </summary>
public abstract class IrInstruction
{
    /// <summary>
    /// The SSA value produced by this instruction, if any.
    /// Terminators and stores typically do not produce a value.
    /// </summary>
    public IrValue? Result { get; set; }

    /// <summary>
    /// Whether this instruction is a terminator (ends a basic block).
    /// </summary>
    public virtual bool IsTerminator => false;

    /// <summary>
    /// Returns the textual IR representation of this instruction.
    /// </summary>
    public abstract string Print();
}
