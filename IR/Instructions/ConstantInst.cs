namespace IR.Instructions;

/// <summary>
/// Materializes a literal constant value (int, string, bool, float, etc.).
/// LLVM-style: produces an SSA value with the constant embedded.
/// </summary>
public class ConstantInst : IrInstruction
{
    public ConstantKind ConstantKind { get; }

    /// <summary>
    /// The raw constant value as a string (e.g. "42", "true", "hello").
    /// </summary>
    public string RawValue { get; }

    /// <summary>
    /// The IR type of the constant.
    /// </summary>
    public IrType ConstantType { get; }

    public ConstantInst(ConstantKind kind, string rawValue, IrType type, IrValue result)
    {
        ConstantKind = kind;
        RawValue = rawValue;
        ConstantType = type;
        Result = result;
    }

    public override string Print()
    {
        var display = ConstantKind == ConstantKind.String ? $"\"{RawValue}\"" : RawValue;
        return $"{Result} = constant {ConstantKind.ToString().ToLowerInvariant()} {display} : {ConstantType}";
    }
}

public enum ConstantKind
{
    Integer,
    Float,
    Double,
    Bool,
    String,
    Char,
    Null
}
