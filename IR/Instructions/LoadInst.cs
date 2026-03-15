namespace IR.Instructions;

/// <summary>
/// Loads a value from an allocated local variable slot.
/// LLVM-style: load from a memory location to produce an SSA value.
/// </summary>
public class LoadInst : IrInstruction
{
    /// <summary>
    /// The source slot to load from (produced by an AllocInst).
    /// </summary>
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
