namespace IR.Instructions;

/// <summary>
/// Integer/float subtraction. Convenience subclass of BinaryInst.
/// LLVM-style: %r = sub i32 %a, %b
/// </summary>
public class SubInst : BinaryInst
{
    public SubInst(IrValue left, IrValue right, IrValue result)
        : base(BinaryOp.Sub, left, right, result) { }

    public override string Print()
    {
        return $"{Result} = sub {Result!.Type} {Left}, {Right}";
    }
}
