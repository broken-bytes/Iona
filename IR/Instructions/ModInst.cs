namespace IR.Instructions;

/// <summary>
/// Integer modulo / remainder. Convenience subclass of BinaryInst.
/// LLVM-style: %r = mod i32 %a, %b
/// </summary>
public class ModInst : BinaryInst
{
    public ModInst(IrValue left, IrValue right, IrValue result)
        : base(BinaryOp.Mod, left, right, result) { }

    public override string Print()
    {
        return $"{Result} = mod {Result!.Type} {Left}, {Right}";
    }
}
