//|--- CallInst.cs ---------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR.Instructions;

public class CallInst : IrInstruction
{
    public string TargetName { get; }

    public List<IrCallArg> Arguments { get; }

    public IrType ReturnType { get; }

    public IrValue? Receiver { get; }

    public bool IsConstructor { get; }

    public CallInst(
        string targetName,
        List<IrCallArg> arguments,
        IrType returnType,
        IrValue? result,
        IrValue? receiver = null,
        bool isConstructor = false)
    {
        TargetName = targetName;
        Arguments = arguments;
        ReturnType = returnType;
        Result = result;
        Receiver = receiver;
        IsConstructor = isConstructor;
    }

    public override string Print()
    {
        var args = string.Join(", ", Arguments.Select(a => $"{a.Name}: {a.Value}"));
        var receiverPart = Receiver != null ? $"{Receiver}." : "";
        var prefix = IsConstructor ? "init_call" : "call";
        if (Result != null)
            return $"{Result} = {prefix} @{receiverPart}{TargetName}({args}) : {ReturnType}";
        return $"{prefix} @{receiverPart}{TargetName}({args}) : {ReturnType}";
    }
}

public class IrCallArg
{
    public string Name { get; }
    public IrValue Value { get; }

    public IrCallArg(string name, IrValue value)
    {
        Name = name;
        Value = value;
    }
}
