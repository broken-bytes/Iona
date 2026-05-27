//|--- PropertyAccessInst.cs -----------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class PropertyAccessInst : IrInstruction
{
    public IrValue Receiver { get; }

    public string PropertyName { get; }

    public IrType PropertyType { get; }

    public bool IsLoad { get; }

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
