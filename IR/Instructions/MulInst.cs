namespace IR.Instructions;

/// <summary>
/// Integer/float multiplication. Convenience subclass of BinaryInst.
/// LLVM-style: %r = mul i32 %a, %b
/// </summary>
public class MulInst : BinaryInst
{
    public MulInst(IrValue left, IrValue right, IrValue result)
        : base(BinaryOp.Mul, left, right, result) { }

    public override string Print()
    {
        return $"{Result} = mul {Result!.Type} {Left}, {Right}";
    }
}
