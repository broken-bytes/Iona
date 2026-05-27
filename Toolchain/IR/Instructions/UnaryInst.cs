//|--- UnaryInst.cs --------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class UnaryInst : IrInstruction
{
    public UnaryOp Op { get; }
    public IrValue Operand { get; }

    public UnaryInst(UnaryOp op, IrValue operand, IrValue result)
    {
        Op = op;
        Operand = operand;
        Result = result;
    }

    public override string Print()
    {
        var opName = Op.ToString().ToLowerInvariant();
        return $"{Result} = {opName} {Result!.Type} {Operand}";
    }
}

public enum UnaryOp
{
    Neg,
    Not,
    BitwiseNot,
    Increment,
    Decrement
}
