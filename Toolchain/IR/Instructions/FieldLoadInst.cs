//|--- FieldLoadInst.cs ----------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class FieldLoadInst : IrInstruction
{
    public IrValue Object { get; }

    public string FieldName { get; }

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
