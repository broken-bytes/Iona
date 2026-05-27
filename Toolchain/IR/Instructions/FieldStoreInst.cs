//|--- FieldStoreInst.cs ---------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class FieldStoreInst : IrInstruction
{
    public IrValue Value { get; }

    public IrValue Object { get; }

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
