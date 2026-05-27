//|--- IrValue.cs ----------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR;

public class IrValue
{
    public int Index { get; }

    public string? DebugName { get; set; }

    public IrType Type { get; }

    public IrValue(int index, IrType type, string? debugName = null)
    {
        Index = index;
        Type = type;
        DebugName = debugName;
    }

    public override string ToString()
    {
        return DebugName != null ? $"%{DebugName}" : $"%{Index}";
    }
}
