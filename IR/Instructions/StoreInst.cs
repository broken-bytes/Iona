namespace IR.Instructions;

/// <summary>
/// Stores a value into an allocated local variable slot.
/// LLVM-style: store value -> destination.
/// </summary>
public class StoreInst : IrInstruction
{
    /// <summary>
    /// The value to store.
    /// </summary>
    public IrValue Value { get; }

    /// <summary>
    /// The destination slot (produced by an AllocInst).
    /// </summary>
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
