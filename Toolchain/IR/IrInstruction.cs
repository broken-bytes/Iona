//|--- IrInstruction.cs ----------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR;

public abstract class IrInstruction
{
    public IrValue? Result { get; set; }

    public virtual bool IsTerminator => false;

    public abstract string Print();
}
