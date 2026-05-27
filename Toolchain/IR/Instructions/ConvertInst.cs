//|--- ConvertInst.cs ------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class ConvertInst : IrInstruction
{
    public IrValue Source { get; }

    public IrType TargetType { get; }

    public ConvertInst(IrValue source, IrType targetType, IrValue result)
    {
        Source = source;
        TargetType = targetType;
        Result = result;
    }

    public override string Print()
    {
        return $"{Result} = convert {Source} : {Source.Type} to {TargetType}";
    }
}
