namespace IR.Instructions;

/// <summary>
/// Comparison operation. Always produces a bool result.
/// LLVM-style: icmp/fcmp unified into a single compare instruction.
/// </summary>
public class CompareInst : IrInstruction
{
    public CompareOp Op { get; }
    public IrValue Left { get; }
    public IrValue Right { get; }

    public CompareInst(CompareOp op, IrValue left, IrValue right, IrValue result)
    {
        Op = op;
        Left = left;
        Right = right;
        Result = result;
    }

    public override string Print()
    {
        var opName = Op.ToString().ToLowerInvariant();
        return $"{Result} = compare {opName} {Left}, {Right} : bool";
    }
}

public enum CompareOp
{
    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual
}
