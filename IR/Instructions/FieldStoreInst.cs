namespace IR.Instructions;

/// <summary>
/// Stores a value into a field of an object or struct. SIL-inspired: retains
/// the field name and owning type.
/// </summary>
public class FieldStoreInst : IrInstruction
{
    /// <summary>
    /// The value to store into the field.
    /// </summary>
    public IrValue Value { get; }

    /// <summary>
    /// The object/struct value whose field is being written.
    /// </summary>
    public IrValue Object { get; }

    /// <summary>
    /// The name of the field to store into.
    /// </summary>
    public string FieldName { get; }

    public FieldStoreInst(IrValue value, IrValue obj, string fieldName)
    {
        Value = value;
        Object = obj;
        FieldName = fieldName;
        // FieldStore does not produce a result value.
    }

    public override string Print()
    {
        return $"field_store {Value} -> {Object}, \"{FieldName}\" : {Value.Type}";
    }
}
