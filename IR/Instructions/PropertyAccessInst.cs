namespace IR.Instructions;

/// <summary>
/// Load or store a property on an object/struct instance.
/// SIL-inspired: property accesses are first-class instructions that preserve
/// the property name and owning type, rather than being lowered to raw
/// offset calculations.
/// </summary>
public class PropertyAccessInst : IrInstruction
{
    /// <summary>
    /// The receiver object (e.g. @self or another SSA value).
    /// </summary>
    public IrValue Receiver { get; }

    /// <summary>
    /// The name of the property being accessed.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// The type of the property.
    /// </summary>
    public IrType PropertyType { get; }

    /// <summary>
    /// Whether this is a load (true) or store (false).
    /// </summary>
    public bool IsLoad { get; }

    /// <summary>
    /// For stores, the value being written. Null for loads.
    /// </summary>
    public IrValue? StoreValue { get; }

    public PropertyAccessInst(
        IrValue receiver,
        string propertyName,
        IrType propertyType,
        bool isLoad,
        IrValue? result,
        IrValue? storeValue = null)
    {
        Receiver = receiver;
        PropertyName = propertyName;
        PropertyType = propertyType;
        IsLoad = isLoad;
        Result = result;
        StoreValue = storeValue;
    }

    public override string Print()
    {
        if (IsLoad)
        {
            return $"{Result} = load_property {Receiver}, #{PropertyName} : {PropertyType}";
        }
        else
        {
            return $"store_property {StoreValue} -> {Receiver}, #{PropertyName} : {PropertyType}";
        }
    }
}
