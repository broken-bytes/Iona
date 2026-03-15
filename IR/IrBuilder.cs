using IR.Instructions;

namespace IR;

/// <summary>
/// LLVM IRBuilder-style helper for emitting instructions into basic blocks.
/// Manages the current insertion point, creates SSA values, and provides
/// convenience methods for creating every instruction type.
///
/// Usage:
///   var builder = new IrBuilder(function);
///   builder.SetInsertPoint(entryBlock);
///   var a = builder.EmitConstantInt(42);
///   var b = builder.EmitConstantInt(10);
///   var sum = builder.EmitAdd(a, b);
///   builder.EmitReturn(sum);
/// </summary>
public class IrBuilder
{
    /// <summary>
    /// The function that this builder is emitting into. Provides SSA value
    /// allocation and block creation.
    /// </summary>
    public IrFunction Function { get; }

    /// <summary>
    /// The current basic block that new instructions will be appended to.
    /// </summary>
    public IrBasicBlock? InsertBlock { get; private set; }

    public IrBuilder(IrFunction function)
    {
        Function = function;
    }

    // ------------------------------------------------------------------
    // Insertion point management
    // ------------------------------------------------------------------

    /// <summary>
    /// Sets the insertion point to the end of the given basic block.
    /// </summary>
    public void SetInsertPoint(IrBasicBlock block)
    {
        InsertBlock = block;
    }

    /// <summary>
    /// Creates a new basic block with the given label, appends it to the
    /// function, and returns it. Does NOT move the insertion point.
    /// </summary>
    public IrBasicBlock CreateBlock(string label)
    {
        return Function.CreateBlock(label);
    }

    /// <summary>
    /// Creates a new basic block and immediately sets it as the insertion point.
    /// </summary>
    public IrBasicBlock CreateAndSetBlock(string label)
    {
        var block = Function.CreateBlock(label);
        SetInsertPoint(block);
        return block;
    }

    /// <summary>
    /// Whether the current insertion block already has a terminator.
    /// </summary>
    public bool IsTerminated => InsertBlock?.IsTerminated ?? true;

    // ------------------------------------------------------------------
    // SSA value allocation
    // ------------------------------------------------------------------

    /// <summary>
    /// Allocates a new SSA value index from the parent function.
    /// </summary>
    public IrValue CreateValue(IrType type, string? debugName = null)
    {
        return Function.CreateValue(type, debugName);
    }

    // ------------------------------------------------------------------
    // Arithmetic instructions
    // ------------------------------------------------------------------

    /// <summary>Emits: %r = add T %left, %right</summary>
    public IrValue EmitAdd(IrValue left, IrValue right, string? name = null)
    {
        var result = CreateValue(left.Type, name);
        Emit(new AddInst(left, right, result));
        return result;
    }

    /// <summary>Emits: %r = sub T %left, %right</summary>
    public IrValue EmitSub(IrValue left, IrValue right, string? name = null)
    {
        var result = CreateValue(left.Type, name);
        Emit(new SubInst(left, right, result));
        return result;
    }

    /// <summary>Emits: %r = mul T %left, %right</summary>
    public IrValue EmitMul(IrValue left, IrValue right, string? name = null)
    {
        var result = CreateValue(left.Type, name);
        Emit(new MulInst(left, right, result));
        return result;
    }

    /// <summary>Emits: %r = div T %left, %right</summary>
    public IrValue EmitDiv(IrValue left, IrValue right, string? name = null)
    {
        var result = CreateValue(left.Type, name);
        Emit(new DivInst(left, right, result));
        return result;
    }

    /// <summary>Emits: %r = mod T %left, %right</summary>
    public IrValue EmitMod(IrValue left, IrValue right, string? name = null)
    {
        var result = CreateValue(left.Type, name);
        Emit(new ModInst(left, right, result));
        return result;
    }

    /// <summary>Emits a generic binary operation.</summary>
    public IrValue EmitBinary(BinaryOp op, IrValue left, IrValue right, string? name = null)
    {
        var result = CreateValue(left.Type, name);
        IrInstruction inst = op switch
        {
            BinaryOp.Add => new AddInst(left, right, result),
            BinaryOp.Sub => new SubInst(left, right, result),
            BinaryOp.Mul => new MulInst(left, right, result),
            BinaryOp.Div => new DivInst(left, right, result),
            BinaryOp.Mod => new ModInst(left, right, result),
            _ => new BinaryInst(op, left, right, result)
        };
        Emit(inst);
        return result;
    }

    // ------------------------------------------------------------------
    // Unary instructions
    // ------------------------------------------------------------------

    /// <summary>Emits a unary operation.</summary>
    public IrValue EmitUnary(UnaryOp op, IrValue operand, string? name = null)
    {
        var result = CreateValue(operand.Type, name);
        Emit(new UnaryInst(op, operand, result));
        return result;
    }

    /// <summary>Emits: %r = neg T %operand</summary>
    public IrValue EmitNeg(IrValue operand, string? name = null)
    {
        return EmitUnary(UnaryOp.Neg, operand, name);
    }

    /// <summary>Emits: %r = not bool %operand</summary>
    public IrValue EmitNot(IrValue operand, string? name = null)
    {
        var result = CreateValue(IrTypes.Bool, name);
        Emit(new UnaryInst(UnaryOp.Not, operand, result));
        return result;
    }

    // ------------------------------------------------------------------
    // Comparison instructions
    // ------------------------------------------------------------------

    /// <summary>Emits: %r = cmp kind T %left, %right</summary>
    public IrValue EmitCmp(CmpKind kind, IrValue left, IrValue right, string? name = null)
    {
        var result = CreateValue(IrTypes.Bool, name);
        Emit(new CmpInst(kind, left, right, result));
        return result;
    }

    /// <summary>Emits: %r = compare op %left, %right : bool</summary>
    public IrValue EmitCompare(CompareOp op, IrValue left, IrValue right, string? name = null)
    {
        var result = CreateValue(IrTypes.Bool, name);
        Emit(new CompareInst(op, left, right, result));
        return result;
    }

    // ------------------------------------------------------------------
    // Memory instructions
    // ------------------------------------------------------------------

    /// <summary>Emits: %r = alloc $name : T</summary>
    public IrValue EmitAlloca(string variableName, IrType type, string? name = null)
    {
        var result = CreateValue(type, name ?? variableName);
        Emit(new AllocInst(variableName, type, result));
        return result;
    }

    /// <summary>Emits: %r = load %source : T</summary>
    public IrValue EmitLoad(IrValue source, IrType type, string? name = null)
    {
        var result = CreateValue(type, name);
        Emit(new LoadInst(source, result));
        return result;
    }

    /// <summary>Emits: store %value -> %destination : T</summary>
    public void EmitStore(IrValue value, IrValue destination)
    {
        Emit(new StoreInst(value, destination));
    }

    // ------------------------------------------------------------------
    // Control flow instructions
    // ------------------------------------------------------------------

    /// <summary>Emits: br label %target</summary>
    public void EmitBranch(IrBasicBlock target)
    {
        Emit(new BranchInst(target));
    }

    /// <summary>Emits: cond_br %cond, label %then, label %else</summary>
    public void EmitCondBranch(IrValue condition, IrBasicBlock thenBlock, IrBasicBlock elseBlock)
    {
        Emit(new CondBranchInst(condition, thenBlock, elseBlock));
    }

    /// <summary>Emits: return %value : T</summary>
    public void EmitReturn(IrValue? value = null)
    {
        Emit(new ReturnInst(value));
    }

    // ------------------------------------------------------------------
    // Constants
    // ------------------------------------------------------------------

    /// <summary>Emits: %r = constant integer N : i32</summary>
    public IrValue EmitConstantInt(int value, string? name = null)
    {
        var result = CreateValue(IrTypes.I32, name);
        Emit(new ConstantInst(ConstantKind.Integer, value.ToString(), IrTypes.I32, result));
        return result;
    }

    /// <summary>Emits: %r = constant integer N : i64</summary>
    public IrValue EmitConstantLong(long value, string? name = null)
    {
        var result = CreateValue(IrTypes.I64, name);
        Emit(new ConstantInst(ConstantKind.Integer, value.ToString(), IrTypes.I64, result));
        return result;
    }

    /// <summary>Emits: %r = constant float N : f32</summary>
    public IrValue EmitConstantFloat(float value, string? name = null)
    {
        var result = CreateValue(IrTypes.F32, name);
        Emit(new ConstantInst(ConstantKind.Float, value.ToString(), IrTypes.F32, result));
        return result;
    }

    /// <summary>Emits: %r = constant double N : f64</summary>
    public IrValue EmitConstantDouble(double value, string? name = null)
    {
        var result = CreateValue(IrTypes.F64, name);
        Emit(new ConstantInst(ConstantKind.Double, value.ToString(), IrTypes.F64, result));
        return result;
    }

    /// <summary>Emits: %r = constant bool V : bool</summary>
    public IrValue EmitConstantBool(bool value, string? name = null)
    {
        var result = CreateValue(IrTypes.Bool, name);
        Emit(new ConstantInst(ConstantKind.Bool, value ? "true" : "false", IrTypes.Bool, result));
        return result;
    }

    /// <summary>Emits: %r = constant string "V" : string</summary>
    public IrValue EmitConstantString(string value, string? name = null)
    {
        var result = CreateValue(IrTypes.String, name);
        Emit(new ConstantInst(ConstantKind.String, value, IrTypes.String, result));
        return result;
    }

    /// <summary>Emits: %r = constant null : void</summary>
    public IrValue EmitConstantNull(IrType type, string? name = null)
    {
        var result = CreateValue(type, name);
        Emit(new ConstantInst(ConstantKind.Null, "null", type, result));
        return result;
    }

    /// <summary>Emits a generic constant instruction.</summary>
    public IrValue EmitConstant(ConstantKind kind, string rawValue, IrType type, string? name = null)
    {
        var result = CreateValue(type, name);
        Emit(new ConstantInst(kind, rawValue, type, result));
        return result;
    }

    // ------------------------------------------------------------------
    // SSA phi nodes
    // ------------------------------------------------------------------

    /// <summary>Emits: %r = phi T [ %v1, %bb1 ], [ %v2, %bb2 ], ...</summary>
    public PhiInst EmitPhi(IrType type, string? name = null)
    {
        var result = CreateValue(type, name);
        var phi = new PhiInst(result);
        Emit(phi);
        return phi;
    }

    // ------------------------------------------------------------------
    // Object / field instructions
    // ------------------------------------------------------------------

    /// <summary>Emits: %r = new_object T</summary>
    public IrValue EmitNewObject(IrType objectType, List<IrCallArg>? ctorArgs = null,
        string? ctorName = null, string? name = null)
    {
        var result = CreateValue(objectType, name);
        Emit(new NewObjectInst(objectType, result, ctorArgs, ctorName));
        return result;
    }

    /// <summary>Emits: %r = field_load %obj, "fieldName" : T</summary>
    public IrValue EmitFieldLoad(IrValue obj, string fieldName, IrType fieldType, string? name = null)
    {
        var result = CreateValue(fieldType, name);
        Emit(new FieldLoadInst(obj, fieldName, fieldType, result));
        return result;
    }

    /// <summary>Emits: field_store %value -> %obj, "fieldName" : T</summary>
    public void EmitFieldStore(IrValue value, IrValue obj, string fieldName)
    {
        Emit(new FieldStoreInst(value, obj, fieldName));
    }

    /// <summary>Emits: %r = load_property %receiver, #prop : T</summary>
    public IrValue EmitPropertyLoad(IrValue receiver, string propertyName, IrType propertyType,
        string? name = null)
    {
        var result = CreateValue(propertyType, name);
        Emit(new PropertyAccessInst(receiver, propertyName, propertyType, isLoad: true, result: result));
        return result;
    }

    /// <summary>Emits: store_property %value -> %receiver, #prop : T</summary>
    public void EmitPropertyStore(IrValue value, IrValue receiver, string propertyName, IrType propertyType)
    {
        Emit(new PropertyAccessInst(receiver, propertyName, propertyType,
            isLoad: false, result: null, storeValue: value));
    }

    // ------------------------------------------------------------------
    // Call instructions
    // ------------------------------------------------------------------

    /// <summary>Emits: %r = call @target(args) : T</summary>
    public IrValue? EmitCall(string targetName, List<IrCallArg> arguments, IrType returnType,
        IrValue? receiver = null, bool isConstructor = false, string? name = null)
    {
        IrValue? result = null;
        if (returnType is not IrPrimitiveType { Primitive: PrimitiveKind.Void })
        {
            result = CreateValue(returnType, name);
        }

        Emit(new CallInst(targetName, arguments, returnType, result, receiver, isConstructor));
        return result;
    }

    /// <summary>Emits a void call: call @target(args) : void</summary>
    public void EmitVoidCall(string targetName, List<IrCallArg> arguments,
        IrValue? receiver = null)
    {
        Emit(new CallInst(targetName, arguments, IrTypes.Void, result: null, receiver: receiver));
    }

    // ------------------------------------------------------------------
    // Conversion
    // ------------------------------------------------------------------

    /// <summary>Emits: %r = convert %source : S to T</summary>
    public IrValue EmitConvert(IrValue source, IrType targetType, string? name = null)
    {
        var result = CreateValue(targetType, name);
        Emit(new ConvertInst(source, targetType, result));
        return result;
    }

    // ------------------------------------------------------------------
    // Internal helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Appends an instruction to the current insertion block.
    /// </summary>
    private void Emit(IrInstruction instruction)
    {
        if (InsertBlock == null)
            throw new InvalidOperationException("No insertion point set. Call SetInsertPoint first.");

        InsertBlock.Append(instruction);
    }
}
