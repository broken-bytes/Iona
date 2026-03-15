namespace IR.Instructions;

/// <summary>
/// A function/method call. Carries the full target name and argument list.
/// SIL-inspired: the call retains the fully qualified function name and type info.
/// </summary>
public class CallInst : IrInstruction
{
    /// <summary>
    /// The fully qualified name of the function being called.
    /// </summary>
    public string TargetName { get; }

    /// <summary>
    /// The arguments passed to the function.
    /// </summary>
    public List<IrCallArg> Arguments { get; }

    /// <summary>
    /// The return type of the called function.
    /// </summary>
    public IrType ReturnType { get; }

    /// <summary>
    /// If this is a method call, the receiver object. Null for free functions.
    /// </summary>
    public IrValue? Receiver { get; }

    /// <summary>
    /// Whether this call is a constructor invocation.
    /// </summary>
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

/// <summary>
/// A named argument in a function call.
/// </summary>
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
