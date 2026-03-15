namespace IR.Instructions;

/// <summary>
/// Loads a field from an object or struct value. SIL-inspired: retains the
/// field name and owning type for high-level type preservation.
/// </summary>
public class FieldLoadInst : IrInstruction
{
    /// <summary>
    /// The object/struct value to load the field from.
    /// </summary>
    public IrValue Object { get; }

    /// <summary>
    /// The name of the field to load.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// The type of the field being loaded.
    /// </summary>
    public IrType FieldType { get; }

    public FieldLoadInst(IrValue obj, string fieldName, IrType fieldType, IrValue result)
    {
        Object = obj;
        FieldName = fieldName;
        FieldType = fieldType;
        Result = result;
    }

    public override string Print()
    {
        return $"{Result} = field_load {Object}, \"{FieldName}\" : {FieldType}";
    }
}
