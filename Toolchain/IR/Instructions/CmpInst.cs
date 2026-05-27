//|--- CmpInst.cs ----------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class CmpInst : CompareInst
{
    public CmpKind Kind { get; }

    public CmpInst(CmpKind kind, IrValue left, IrValue right, IrValue result)
        : base(MapKind(kind), left, right, result)
    {
        Kind = kind;
    }

    public override string Print()
    {
        var kindName = Kind.ToString().ToLowerInvariant();
        return $"{Result} = cmp {kindName} {Left.Type} {Left}, {Right}";
    }

    private static CompareOp MapKind(CmpKind kind) => kind switch
    {
        CmpKind.Eq => CompareOp.Equal,
        CmpKind.Ne => CompareOp.NotEqual,
        CmpKind.Lt => CompareOp.LessThan,
        CmpKind.Le => CompareOp.LessThanOrEqual,
        CmpKind.Gt => CompareOp.GreaterThan,
        CmpKind.Ge => CompareOp.GreaterThanOrEqual,
        _ => CompareOp.Equal
    };
}

public enum CmpKind
{
    Eq,
    Ne,
    Lt,
    Le,
    Gt,
    Ge
}
