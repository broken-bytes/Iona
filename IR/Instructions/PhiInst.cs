namespace IR.Instructions;

/// <summary>
/// SSA phi node. Merges values from different predecessor basic blocks.
/// LLVM-style: %x = phi i32 [ %a, %bb1 ], [ %b, %bb2 ]
/// </summary>
public class PhiInst : IrInstruction
{
    /// <summary>
    /// The incoming (value, block) pairs. Each entry specifies which value
    /// flows in from which predecessor block.
    /// </summary>
    public List<PhiIncoming> Incomings { get; } = new();

    public PhiInst(IrValue result)
    {
        Result = result;
    }

    /// <summary>
    /// Adds an incoming value from the given predecessor block.
    /// </summary>
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

/// <summary>
/// A single incoming edge for a phi node: a value and the block it comes from.
/// </summary>
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
