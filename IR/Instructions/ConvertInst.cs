namespace IR.Instructions;

/// <summary>
/// Converts a value from one type to another. Covers numeric widening/narrowing,
/// int-to-float, float-to-int, and other type conversions.
/// SIL-inspired: the conversion retains both source and target types.
/// </summary>
public class ConvertInst : IrInstruction
{
    /// <summary>
    /// The value to convert.
    /// </summary>
    public IrValue Source { get; }

    /// <summary>
    /// The target type to convert to.
    /// </summary>
    public IrType TargetType { get; }

    public ConvertInst(IrValue source, IrType targetType, IrValue result)
    {
        Source = source;
        TargetType = targetType;
        Result = result;
    }

    public override string Print()
    {
        return $"{Result} = convert {Source} : {Source.Type} to {TargetType}";
    }
}
