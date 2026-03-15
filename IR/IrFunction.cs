namespace IR;

/// <summary>
/// An IR function. Contains a list of basic blocks forming the control flow graph.
/// The first block is always the entry block.
///
/// SIL-inspired: functions carry their full type signature and retain the
/// owning type identity (for methods).
/// LLVM-inspired: the body is a flat list of basic blocks in SSA form.
/// </summary>
public class IrFunction
{
    /// <summary>
    /// The fully qualified name of this function (e.g. "TestModule.Foo.greet").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The return type of this function.
    /// </summary>
    public IrType ReturnType { get; }

    /// <summary>
    /// The parameters of this function.
    /// </summary>
    public List<IrParameter> Parameters { get; } = new();

    /// <summary>
    /// Whether this is an instance method (has an implicit @self parameter).
    /// </summary>
    public bool IsInstance { get; set; }

    /// <summary>
    /// Whether this function is a constructor (.ctor).
    /// </summary>
    public bool IsConstructor { get; set; }

    /// <summary>
    /// The owning type, if this is a method. Null for free functions.
    /// </summary>
    public IrType? OwningType { get; set; }

    /// <summary>
    /// The ordered list of basic blocks. The first block is the entry block.
    /// </summary>
    public List<IrBasicBlock> Blocks { get; } = new();

    /// <summary>
    /// Counter for generating unique SSA value indices within this function.
    /// </summary>
    private int _nextValueIndex;

    public IrFunction(string name, IrType returnType)
    {
        Name = name;
        ReturnType = returnType;
    }

    /// <summary>
    /// The entry basic block (first block).
    /// </summary>
    public IrBasicBlock EntryBlock => Blocks[0];

    /// <summary>
    /// Creates a new basic block with the given label and appends it to this function.
    /// </summary>
    public IrBasicBlock CreateBlock(string label)
    {
        var block = new IrBasicBlock(label);
        Blocks.Add(block);
        return block;
    }

    /// <summary>
    /// Allocates the next SSA value index for this function.
    /// </summary>
    public IrValue CreateValue(IrType type, string? debugName = null)
    {
        return new IrValue(_nextValueIndex++, type, debugName);
    }
}

/// <summary>
/// A function parameter with a name and type.
/// </summary>
public class IrParameter
{
    public string Name { get; }
    public IrType Type { get; }

    /// <summary>
    /// The SSA value representing this parameter inside the function body.
    /// </summary>
    public IrValue Value { get; }

    public IrParameter(string name, IrType type, IrValue value)
    {
        Name = name;
        Type = type;
        Value = value;
    }
}
