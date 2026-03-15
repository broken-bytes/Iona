namespace IR.Instructions;

/// <summary>
/// Allocates storage for a local variable. The result is a reference to the
/// allocated slot (like LLVM's alloca). The variable name is preserved for
/// debugging and readability.
/// </summary>
public class AllocInst : IrInstruction
{
    /// <summary>
    /// The name of the local variable being allocated.
    /// </summary>
    public string VariableName { get; }

    /// <summary>
    /// The type of value that will be stored in this slot.
    /// </summary>
    public IrType AllocType { get; }

    public AllocInst(string variableName, IrType allocType, IrValue result)
    {
        VariableName = variableName;
        AllocType = allocType;
        Result = result;
    }

    public override string Print()
    {
        return $"{Result} = alloc ${VariableName} : {AllocType}";
    }
}
