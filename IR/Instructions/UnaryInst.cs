namespace IR.Instructions;

/// <summary>
/// Unary operation (negation, logical not, bitwise not, increment, decrement).
/// LLVM-style: %r = neg i32 %a
/// </summary>
public class UnaryInst : IrInstruction
{
    public UnaryOp Op { get; }
    public IrValue Operand { get; }

    public UnaryInst(UnaryOp op, IrValue operand, IrValue result)
    {
        Op = op;
        Operand = operand;
        Result = result;
    }

    public override string Print()
    {
        var opName = Op.ToString().ToLowerInvariant();
        return $"{Result} = {opName} {Result!.Type} {Operand}";
    }
}

public enum UnaryOp
{
    Neg,
    Not,
    BitwiseNot,
    Increment,
    Decrement
}
