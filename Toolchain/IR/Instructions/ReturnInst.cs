//|--- ReturnInst.cs -------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class ReturnInst : IrInstruction
{
    public IrValue? ReturnValue { get; }

    public override bool IsTerminator => true;

    public ReturnInst(IrValue? returnValue = null)
    {
        ReturnValue = returnValue;
    }

    public override string Print()
    {
        if (ReturnValue != null)
            return $"return {ReturnValue} : {ReturnValue.Type}";
        return "return void";
    }
}
