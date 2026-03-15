namespace IR.Instructions;

/// <summary>
/// Integer/float division. Convenience subclass of BinaryInst.
/// LLVM-style: %r = div i32 %a, %b
/// </summary>
public class DivInst : BinaryInst
{
    public DivInst(IrValue left, IrValue right, IrValue result)
        : base(BinaryOp.Div, left, right, result) { }

    public override string Print()
    {
        return $"{Result} = div {Result!.Type} {Left}, {Right}";
    }
}
