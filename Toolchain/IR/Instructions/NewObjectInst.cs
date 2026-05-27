//|--- NewObjectInst.cs ----------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class NewObjectInst : IrInstruction
{
    public IrType ObjectType { get; }

    public List<IrCallArg> ConstructorArgs { get; }

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
