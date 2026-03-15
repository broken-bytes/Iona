namespace IR.Instructions;

/// <summary>
/// Integer/float addition. Convenience subclass of BinaryInst.
/// LLVM-style: %r = add i32 %a, %b
/// </summary>
public class AddInst : BinaryInst
{
    public AddInst(IrValue left, IrValue right, IrValue result)
        : base(BinaryOp.Add, left, right, result) { }

    public override string Print()
    {
        return $"{Result} = add {Result!.Type} {Left}, {Right}";
    }
}
