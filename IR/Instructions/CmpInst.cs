namespace IR.Instructions;

/// <summary>
/// Comparison instruction with a CmpKind enum. Convenience subclass of CompareInst
/// that uses the CmpKind naming convention. Always produces a bool result.
/// LLVM-style: %r = cmp eq i32 %a, %b
/// </summary>
public class CmpInst : CompareInst
{
    /// <summary>
    /// The comparison kind, using the short-form naming convention.
    /// </summary>
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

/// <summary>
/// Short-form comparison kinds (LLVM-style: eq, ne, lt, le, gt, ge).
/// </summary>
public enum CmpKind
{
    Eq,
    Ne,
    Lt,
    Le,
    Gt,
    Ge
}
