//|--- AllocInst.cs --------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class AllocInst : IrInstruction
{
    public string VariableName { get; }

    public IrType AllocType { get; }

    public AllocInst(string variableName, IrType allocType, IrValue result)
    {
        VariableName = variableName;
        AllocType = allocType;
        Result = result;
    }

    public override string Print()
    {
        return $"{Result} = alloc ${VariableName} : {AllocType}";
    }
}
