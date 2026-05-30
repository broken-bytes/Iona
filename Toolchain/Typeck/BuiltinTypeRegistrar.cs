//|--- BuiltinTypeRegistrar.cs ---------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Types;
using Symbols;
using Symbols.Symbols;

namespace Typeck;

public static class BuiltinTypeRegistrar
{
    public static void RegisterBuiltins(SymbolTable table)
    {
        // Create module hierarchy: Iona -> Builtins
        var ionaModule = new ModuleSymbol("Iona", "Iona.Builtins");
        table.AddModule(ionaModule);

        var builtinsModule = new ModuleSymbol("Builtins", "Iona.Builtins");
        ionaModule.AddSymbol(builtinsModule);

        // Void (no operators)
        var voidType = new TypeSymbol("Void", TypeKind.Struct);
        builtinsModule.AddSymbol(voidType);

        // Bool
        var boolType = new TypeSymbol("Bool", TypeKind.Struct);
        builtinsModule.AddSymbol(boolType);
        AddBinaryOp(boolType, OperatorType.Equal, boolType, boolType);
        AddBinaryOp(boolType, OperatorType.NotEqual, boolType, boolType);
        AddBinaryOp(boolType, OperatorType.And, boolType, boolType);
        AddBinaryOp(boolType, OperatorType.Or, boolType, boolType);

        // Char
        var charType = new TypeSymbol("Char", TypeKind.Struct);
        builtinsModule.AddSymbol(charType);
        AddBinaryOp(charType, OperatorType.Equal, charType, boolType);
        AddBinaryOp(charType, OperatorType.NotEqual, charType, boolType);

        // Integer types
        var int8Type = RegisterNumericType(builtinsModule, "Int8", boolType);
        var int16Type = RegisterNumericType(builtinsModule, "Int16", boolType);
        var int32Type = RegisterNumericType(builtinsModule, "Int32", boolType);
        var int64Type = RegisterNumericType(builtinsModule, "Int64", boolType);
        var nintType = RegisterNumericType(builtinsModule, "NInt", boolType);

        // Unsigned integer types
        var uint8Type = RegisterNumericType(builtinsModule, "UInt8", boolType);
        var uint16Type = RegisterNumericType(builtinsModule, "UInt16", boolType);
        var uint32Type = RegisterNumericType(builtinsModule, "UInt32", boolType);
        var uint64Type = RegisterNumericType(builtinsModule, "UInt64", boolType);

        // Floating point types
        var floatType = RegisterNumericType(builtinsModule, "Float", boolType);
        var doubleType = RegisterNumericType(builtinsModule, "Double", boolType);

        // String (reference type)
        var stringType = new TypeSymbol("String", TypeKind.Class);
        builtinsModule.AddSymbol(stringType);
        AddBinaryOp(stringType, OperatorType.Add, stringType, stringType);
        AddBinaryOp(stringType, OperatorType.Equal, stringType, boolType);
        AddBinaryOp(stringType, OperatorType.NotEqual, stringType, boolType);

        // Cross-type operators (Int32 + Float → Float, etc.)
        AddBinaryOp(int32Type, OperatorType.Add, floatType, floatType);
        AddBinaryOp(int32Type, OperatorType.Subtract, floatType, floatType);
        AddBinaryOp(int32Type, OperatorType.Multiply, floatType, floatType);
        AddBinaryOp(int32Type, OperatorType.Divide, floatType, floatType);

        // Generic collection types — registered as open generics under Iona.Builtins. Codegen
        // maps these to System.Collections.Generic.List<T> / Dictionary<K,V> / HashSet<T>.
        builtinsModule.AddSymbol(new TypeSymbol("List", TypeKind.Class));
        builtinsModule.AddSymbol(new TypeSymbol("Map", TypeKind.Class));
        builtinsModule.AddSymbol(new TypeSymbol("Set", TypeKind.Class));
    }

    private static TypeSymbol RegisterNumericType(ModuleSymbol parent, string name, TypeSymbol boolType)
    {
        var type = new TypeSymbol(name, TypeKind.Struct);
        parent.AddSymbol(type);

        // Arithmetic operators: (Self, Self) → Self
        AddBinaryOp(type, OperatorType.Add, type, type);
        AddBinaryOp(type, OperatorType.Subtract, type, type);
        AddBinaryOp(type, OperatorType.Multiply, type, type);
        AddBinaryOp(type, OperatorType.Divide, type, type);
        AddBinaryOp(type, OperatorType.Modulo, type, type);

        // Comparison operators: (Self, Self) → Bool
        AddBinaryOp(type, OperatorType.Equal, type, boolType);
        AddBinaryOp(type, OperatorType.NotEqual, type, boolType);
        AddBinaryOp(type, OperatorType.LessThan, type, boolType);
        AddBinaryOp(type, OperatorType.GreaterThan, type, boolType);
        AddBinaryOp(type, OperatorType.LessThanOrEqual, type, boolType);
        AddBinaryOp(type, OperatorType.GreaterThanOrEqual, type, boolType);

        return type;
    }

    private static void AddBinaryOp(TypeSymbol ownerType, OperatorType opType, TypeSymbol operandType, TypeSymbol returnType)
    {
        var op = new OperatorSymbol(opType) { ReturnType = returnType };
        var left = new ParameterSymbol("left", ownerType, op);
        var right = new ParameterSymbol("right", operandType, op);
        op.AddSymbol(left);
        op.AddSymbol(right);
        ownerType.AddSymbol(op);
    }
}
