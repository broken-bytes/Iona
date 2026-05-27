//|--- IrFunction.cs -------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR;

public class IrFunction
{
    public string Name { get; }

    public IrType ReturnType { get; }

    public List<IrParameter> Parameters { get; } = new();

    public bool IsInstance { get; set; }

    public bool IsConstructor { get; set; }

    public IrType? OwningType { get; set; }

    public List<IrBasicBlock> Blocks { get; } = new();

    private int _nextValueIndex;

    public IrFunction(string name, IrType returnType)
    {
        Name = name;
        ReturnType = returnType;
    }

    public IrBasicBlock EntryBlock => Blocks[0];

    public IrBasicBlock CreateBlock(string label)
    {
        var block = new IrBasicBlock(label);
        Blocks.Add(block);
        return block;
    }

    public IrValue CreateValue(IrType type, string? debugName = null)
    {
        return new IrValue(_nextValueIndex++, type, debugName);
    }
}

public class IrParameter
{
    public string Name { get; }
    public IrType Type { get; }

    public IrValue Value { get; }

    public IrParameter(string name, IrType type, IrValue value)
    {
        Name = name;
        Type = type;
        Value = value;
    }
}
