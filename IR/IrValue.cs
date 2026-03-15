namespace IR;

/// <summary>
/// An SSA value (register / temporary). Every instruction that produces a result
/// creates exactly one IrValue. Values are immutable once assigned (SSA property).
///
/// LLVM-inspired: values have a numeric name (%0, %1, ...) and a type.
/// SIL-inspired: named values are also supported for readability (%sum, %self).
/// </summary>
public class IrValue
{
    /// <summary>
    /// The numeric index of this value within its function.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// An optional human-readable name for this value (e.g. "sum", "self").
    /// When null, the value is referred to by its index (%0, %1, ...).
    /// </summary>
    public string? DebugName { get; set; }

    /// <summary>
    /// The IR type of this value.
    /// </summary>
    public IrType Type { get; }

    public IrValue(int index, IrType type, string? debugName = null)
    {
        Index = index;
        Type = type;
        DebugName = debugName;
    }

    /// <summary>
    /// Returns the textual representation of this value (e.g. "%0" or "%sum").
    /// </summary>
    public override string ToString()
    {
        return DebugName != null ? $"%{DebugName}" : $"%{Index}";
    }
}
