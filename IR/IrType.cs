using AST.Types;

namespace IR;

/// <summary>
/// Represents a type in the IR. Preserves high-level type identity (SIL-inspired)
/// while providing a flat, uniform representation (LLVM-inspired).
/// </summary>
public abstract class IrType
{
    public abstract string Name { get; }

    public abstract override string ToString();

    public override bool Equals(object? obj)
    {
        if (obj is IrType other)
            return ToString() == other.ToString();
        return false;
    }

    public override int GetHashCode() => ToString().GetHashCode();
}

/// <summary>
/// Primitive types: i32, i64, f32, f64, bool, string, void, char.
/// </summary>
public sealed class IrPrimitiveType : IrType
{
    public PrimitiveKind Primitive { get; }

    public override string Name => Primitive switch
    {
        PrimitiveKind.I32 => "i32",
        PrimitiveKind.I64 => "i64",
        PrimitiveKind.F32 => "f32",
        PrimitiveKind.F64 => "f64",
        PrimitiveKind.Bool => "bool",
        PrimitiveKind.String => "string",
        PrimitiveKind.Char => "char",
        PrimitiveKind.Void => "void",
        _ => "unknown"
    };

    public IrPrimitiveType(PrimitiveKind kind)
    {
        Primitive = kind;
    }

    public override string ToString() => Name;
}

public enum PrimitiveKind
{
    I32,
    I64,
    F32,
    F64,
    Bool,
    String,
    Char,
    Void
}

/// <summary>
/// A named type reference to a class. Preserves the fully qualified name
/// so the type identity is not erased (SIL-style).
/// </summary>
public sealed class IrClassType : IrType
{
    public string FullyQualifiedName { get; }
    public override string Name => FullyQualifiedName;

    public IrClassType(string fullyQualifiedName)
    {
        FullyQualifiedName = fullyQualifiedName;
    }

    public override string ToString() => $"%{FullyQualifiedName}";
}

/// <summary>
/// A named type reference to a struct. Value semantics are preserved (SIL-style).
/// </summary>
public sealed class IrStructType : IrType
{
    public string FullyQualifiedName { get; }
    public override string Name => FullyQualifiedName;

    public IrStructType(string fullyQualifiedName)
    {
        FullyQualifiedName = fullyQualifiedName;
    }

    public override string ToString() => $"%{FullyQualifiedName}";
}

/// <summary>
/// A named type reference to an enum.
/// </summary>
public sealed class IrEnumType : IrType
{
    public string FullyQualifiedName { get; }
    public override string Name => FullyQualifiedName;

    public IrEnumType(string fullyQualifiedName)
    {
        FullyQualifiedName = fullyQualifiedName;
    }

    public override string ToString() => $"%{FullyQualifiedName}";
}

/// <summary>
/// An array type wrapping an element type.
/// </summary>
public sealed class IrArrayType : IrType
{
    public IrType ElementType { get; }
    public override string Name => $"[{ElementType}]";

    public IrArrayType(IrType elementType)
    {
        ElementType = elementType;
    }

    public override string ToString() => $"[{ElementType}]";
}

/// <summary>
/// Provides well-known type singletons and helpers for mapping AST types to IR types.
/// </summary>
public static class IrTypes
{
    public static readonly IrPrimitiveType I32 = new(PrimitiveKind.I32);
    public static readonly IrPrimitiveType I64 = new(PrimitiveKind.I64);
    public static readonly IrPrimitiveType F32 = new(PrimitiveKind.F32);
    public static readonly IrPrimitiveType F64 = new(PrimitiveKind.F64);
    public static readonly IrPrimitiveType Bool = new(PrimitiveKind.Bool);
    public static readonly IrPrimitiveType String = new(PrimitiveKind.String);
    public static readonly IrPrimitiveType Char = new(PrimitiveKind.Char);
    public static readonly IrPrimitiveType Void = new(PrimitiveKind.Void);

    /// <summary>
    /// Maps a TypeReferenceNode from the AST (with resolved types) into an IrType.
    /// </summary>
    public static IrType FromTypeReference(AST.Nodes.TypeReferenceNode? typeRef)
    {
        if (typeRef == null)
            return Void;

        // Check for array types
        if (typeRef.ReferenceKind == AST.Nodes.TypeReferenceKind.Array)
        {
            var elementTypeRef = typeRef.GenericArguments.FirstOrDefault();
            var elementType = elementTypeRef is AST.Nodes.TypeReferenceNode inner
                ? FromTypeReference(inner)
                : I32; // fallback
            return new IrArrayType(elementType);
        }

        return MapByFqn(typeRef.FullyQualifiedName, typeRef.TypeKind);
    }

    /// <summary>
    /// Maps a fully qualified name and type kind to an IrType.
    /// </summary>
    public static IrType MapByFqn(string fqn, Kind typeKind)
    {
        // Map well-known Iona built-in primitives
        switch (fqn)
        {
            case "Iona.Builtins.Int":
            case "Iona.Builtins.Int32":
            case "Int":
            case "Int32":
            case "int":
                return I32;

            case "Iona.Builtins.Int64":
            case "Int64":
                return I64;

            case "Iona.Builtins.Float":
            case "Iona.Builtins.Float32":
            case "Float":
            case "Float32":
            case "float":
                return F32;

            case "Iona.Builtins.Float64":
            case "Iona.Builtins.Double":
            case "Float64":
            case "Double":
            case "double":
                return F64;

            case "Iona.Builtins.Bool":
            case "Bool":
            case "bool":
                return Bool;

            case "Iona.Builtins.String":
            case "String":
            case "string":
                return String;

            case "Iona.Builtins.Char":
            case "Char":
            case "char":
                return Char;

            case "Iona.Builtins.Void":
            case "Void":
            case "void":
            case "":
                return Void;
        }

        // Not a primitive -- create a named type based on the type kind
        return typeKind switch
        {
            Kind.Class => new IrClassType(fqn),
            Kind.Struct => new IrStructType(fqn),
            Kind.Enum => new IrEnumType(fqn),
            _ => new IrClassType(fqn) // default to class reference for unknown kinds
        };
    }
}
