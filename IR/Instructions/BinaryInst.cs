namespace IR.Instructions;

/// <summary>
/// Binary arithmetic/logical operation. LLVM-style: simple, flat, typed.
/// </summary>
public class BinaryInst : IrInstruction
{
    public BinaryOp Op { get; }
    public IrValue Left { get; }
    public IrValue Right { get; }

    public BinaryInst(BinaryOp op, IrValue left, IrValue right, IrValue result)
    {
        Op = op;
        Left = left;
        Right = right;
        Result = result;
    }

    public override string Print()
    {
        var opName = Op.ToString().ToLowerInvariant();
        return $"{Result} = binary {opName} {Left}, {Right} : {Result!.Type}";
    }
}

public enum BinaryOp
{
    Add,
    Sub,
    Mul,
    Div,
    Mod,
    And,
    Or
}
