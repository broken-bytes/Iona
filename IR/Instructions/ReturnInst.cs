namespace IR.Instructions;

/// <summary>
/// Return from function. A terminator instruction.
/// LLVM-style: explicit return with optional value.
/// </summary>
public class ReturnInst : IrInstruction
{
    /// <summary>
    /// The value to return. Null for void returns.
    /// </summary>
    public IrValue? ReturnValue { get; }

    public override bool IsTerminator => true;

    public ReturnInst(IrValue? returnValue = null)
    {
        ReturnValue = returnValue;
    }

    public override string Print()
    {
        if (ReturnValue != null)
            return $"return {ReturnValue} : {ReturnValue.Type}";
        return "return void";
    }
}
