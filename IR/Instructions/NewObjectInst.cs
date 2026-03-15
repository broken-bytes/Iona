namespace IR.Instructions;

/// <summary>
/// Allocates a new object instance of a class or struct type. SIL-inspired:
/// the allocation retains the full type identity. The result is an uninitialized
/// reference; a subsequent init_call fills in the fields.
/// </summary>
public class NewObjectInst : IrInstruction
{
    /// <summary>
    /// The type of the object being allocated.
    /// </summary>
    public IrType ObjectType { get; }

    /// <summary>
    /// Arguments passed to the constructor (if allocation and init are fused).
    /// </summary>
    public List<IrCallArg> ConstructorArgs { get; }

    /// <summary>
    /// The fully qualified name of the constructor, if any.
    /// </summary>
    public string? ConstructorName { get; }

    public NewObjectInst(IrType objectType, IrValue result,
        List<IrCallArg>? constructorArgs = null, string? constructorName = null)
    {
        ObjectType = objectType;
        Result = result;
        ConstructorArgs = constructorArgs ?? new List<IrCallArg>();
        ConstructorName = constructorName;
    }

    public override string Print()
    {
        if (ConstructorArgs.Count > 0)
        {
            var args = string.Join(", ", ConstructorArgs.Select(a => $"{a.Name}: {a.Value}"));
            var ctor = ConstructorName != null ? $" @{ConstructorName}" : "";
            return $"{Result} = new_object {ObjectType}{ctor}({args})";
        }

        return $"{Result} = new_object {ObjectType}";
    }
}
