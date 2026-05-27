//|--- IrBuilder.cs --------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using IR.Instructions;

namespace IR;

public class IrBuilder
{
    public IrFunction Function { get; }

    public IrBasicBlock? InsertBlock { get; private set; }

    public IrBuilder(IrFunction function)
    {
        Function = function;
    }

    // ------------------------------------------------------------------
    // Insertion point management
    // ------------------------------------------------------------------

    public void SetInsertPoint(IrBasicBlock block)
    {
        InsertBlock = block;
    }

    public IrBasicBlock CreateBlock(string label)
    {
        return Function.CreateBlock(label);
    }

    public IrBasicBlock CreateAndSetBlock(string label)
    {
        var block = Function.CreateBlock(label);
        SetInsertPoint(block);
        return block;
    }

    public bool IsTerminated => InsertBlock?.IsTerminated ?? true;

    // ------------------------------------------------------------------
    // SSA value allocation
    // ------------------------------------------------------------------

    public IrValue CreateValue(IrType type, string? debugName = null)
    {
        return Function.CreateValue(type, debugName);
    }

    // ------------------------------------------------------------------
    // Arithmetic instructions
    // ------------------------------------------------------------------

    public IrValue EmitAdd(IrValue left, IrValue right, string? name = null)
    {
        var result = CreateValue(left.Type, name);
        Emit(new AddInst(left, right, result));
        return result;
    }

    public IrValue EmitSub(IrValue left, IrValue right, string? name = null)
    {
        var result = CreateValue(left.Type, name);
        Emit(new SubInst(left, right, result));
        return result;
    }

    public IrValue EmitMul(IrValue left, IrValue right, string? name = null)
    {
        var result = CreateValue(left.Type, name);
        Emit(new MulInst(left, right, result));
        return result;
    }

    public IrValue EmitDiv(IrValue left, IrValue right, string? name = null)
    {
        var result = CreateValue(left.Type, name);
        Emit(new DivInst(left, right, result));
        return result;
    }

    public IrValue EmitMod(IrValue left, IrValue right, string? name = null)
    {
        var result = CreateValue(left.Type, name);
        Emit(new ModInst(left, right, result));
        return result;
    }

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

    public IrValue EmitUnary(UnaryOp op, IrValue operand, string? name = null)
    {
        var result = CreateValue(operand.Type, name);
        Emit(new UnaryInst(op, operand, result));
        return result;
    }

    public IrValue EmitNeg(IrValue operand, string? name = null)
    {
        return EmitUnary(UnaryOp.Neg, operand, name);
    }

    public IrValue EmitNot(IrValue operand, string? name = null)
    {
        var result = CreateValue(IrTypes.Bool, name);
        Emit(new UnaryInst(UnaryOp.Not, operand, result));
        return result;
    }

    // ------------------------------------------------------------------
    // Comparison instructions
    // ------------------------------------------------------------------

    public IrValue EmitCmp(CmpKind kind, IrValue left, IrValue right, string? name = null)
    {
        var result = CreateValue(IrTypes.Bool, name);
        Emit(new CmpInst(kind, left, right, result));
        return result;
    }

    public IrValue EmitCompare(CompareOp op, IrValue left, IrValue right, string? name = null)
    {
        var result = CreateValue(IrTypes.Bool, name);
        Emit(new CompareInst(op, left, right, result));
        return result;
    }

    // ------------------------------------------------------------------
    // Memory instructions
    // ------------------------------------------------------------------

    public IrValue EmitAlloca(string variableName, IrType type, string? name = null)
    {
        var result = CreateValue(type, name ?? variableName);
        Emit(new AllocInst(variableName, type, result));
        return result;
    }

    public IrValue EmitLoad(IrValue source, IrType type, string? name = null)
    {
        var result = CreateValue(type, name);
        Emit(new LoadInst(source, result));
        return result;
    }

    public void EmitStore(IrValue value, IrValue destination)
    {
        Emit(new StoreInst(value, destination));
    }

    // ------------------------------------------------------------------
    // Control flow instructions
    // ------------------------------------------------------------------

    public void EmitBranch(IrBasicBlock target)
    {
        Emit(new BranchInst(target));
    }

    public void EmitCondBranch(IrValue condition, IrBasicBlock thenBlock, IrBasicBlock elseBlock)
    {
        Emit(new CondBranchInst(condition, thenBlock, elseBlock));
    }

    public void EmitReturn(IrValue? value = null)
    {
        Emit(new ReturnInst(value));
    }

    // ------------------------------------------------------------------
    // Constants
    // ------------------------------------------------------------------

    public IrValue EmitConstantInt(int value, string? name = null)
    {
        var result = CreateValue(IrTypes.I32, name);
        Emit(new ConstantInst(ConstantKind.Integer, value.ToString(), IrTypes.I32, result));
        return result;
    }

    public IrValue EmitConstantLong(long value, string? name = null)
    {
        var result = CreateValue(IrTypes.I64, name);
        Emit(new ConstantInst(ConstantKind.Integer, value.ToString(), IrTypes.I64, result));
        return result;
    }

    public IrValue EmitConstantFloat(float value, string? name = null)
    {
        var result = CreateValue(IrTypes.F32, name);
        Emit(new ConstantInst(ConstantKind.Float, value.ToString(), IrTypes.F32, result));
        return result;
    }

    public IrValue EmitConstantDouble(double value, string? name = null)
    {
        var result = CreateValue(IrTypes.F64, name);
        Emit(new ConstantInst(ConstantKind.Double, value.ToString(), IrTypes.F64, result));
        return result;
    }

    public IrValue EmitConstantBool(bool value, string? name = null)
    {
        var result = CreateValue(IrTypes.Bool, name);
        Emit(new ConstantInst(ConstantKind.Bool, value ? "true" : "false", IrTypes.Bool, result));
        return result;
    }

    public IrValue EmitConstantString(string value, string? name = null)
    {
        var result = CreateValue(IrTypes.String, name);
        Emit(new ConstantInst(ConstantKind.String, value, IrTypes.String, result));
        return result;
    }

    public IrValue EmitConstantNull(IrType type, string? name = null)
    {
        var result = CreateValue(type, name);
        Emit(new ConstantInst(ConstantKind.Null, "null", type, result));
        return result;
    }

    public IrValue EmitConstant(ConstantKind kind, string rawValue, IrType type, string? name = null)
    {
        var result = CreateValue(type, name);
        Emit(new ConstantInst(kind, rawValue, type, result));
        return result;
    }

    // ------------------------------------------------------------------
    // SSA phi nodes
    // ------------------------------------------------------------------

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

    public IrValue EmitNewObject(IrType objectType, List<IrCallArg>? ctorArgs = null,
        string? ctorName = null, string? name = null)
    {
        var result = CreateValue(objectType, name);
        Emit(new NewObjectInst(objectType, result, ctorArgs, ctorName));
        return result;
    }

    public IrValue EmitFieldLoad(IrValue obj, string fieldName, IrType fieldType, string? name = null)
    {
        var result = CreateValue(fieldType, name);
        Emit(new FieldLoadInst(obj, fieldName, fieldType, result));
        return result;
    }

    public void EmitFieldStore(IrValue value, IrValue obj, string fieldName)
    {
        Emit(new FieldStoreInst(value, obj, fieldName));
    }

    public IrValue EmitPropertyLoad(IrValue receiver, string propertyName, IrType propertyType,
        string? name = null)
    {
        var result = CreateValue(propertyType, name);
        Emit(new PropertyAccessInst(receiver, propertyName, propertyType, isLoad: true, result: result));
        return result;
    }

    public void EmitPropertyStore(IrValue value, IrValue receiver, string propertyName, IrType propertyType)
    {
        Emit(new PropertyAccessInst(receiver, propertyName, propertyType,
            isLoad: false, result: null, storeValue: value));
    }

    // ------------------------------------------------------------------
    // Call instructions
    // ------------------------------------------------------------------

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

    public void EmitVoidCall(string targetName, List<IrCallArg> arguments,
        IrValue? receiver = null)
    {
        Emit(new CallInst(targetName, arguments, IrTypes.Void, result: null, receiver: receiver));
    }

    // ------------------------------------------------------------------
    // Conversion
    // ------------------------------------------------------------------

    public IrValue EmitConvert(IrValue source, IrType targetType, string? name = null)
    {
        var result = CreateValue(targetType, name);
        Emit(new ConvertInst(source, targetType, result));
        return result;
    }

    // ------------------------------------------------------------------
    // Internal helpers
    // ------------------------------------------------------------------

    private void Emit(IrInstruction instruction)
    {
        if (InsertBlock == null)
            throw new InvalidOperationException("No insertion point set. Call SetInsertPoint first.");

        InsertBlock.Append(instruction);
    }
}
