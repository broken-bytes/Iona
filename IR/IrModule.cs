namespace IR;

/// <summary>
/// Top-level IR container, analogous to an LLVM Module. Holds all functions
/// and type declarations for a compilation unit.
/// </summary>
public class IrModule
{
    /// <summary>
    /// The name of this module (e.g. "TestModule").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// All functions defined in this module.
    /// </summary>
    public List<IrFunction> Functions { get; } = new();

    /// <summary>
    /// Type declarations in this module (classes, structs, enums).
    /// Preserves high-level type identity (SIL-style).
    /// </summary>
    public List<IrTypeDecl> TypeDeclarations { get; } = new();

    public IrModule(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Adds a function to this module.
    /// </summary>
    public void AddFunction(IrFunction function)
    {
        Functions.Add(function);
    }

    /// <summary>
    /// Adds a type declaration to this module.
    /// </summary>
    public void AddTypeDeclaration(IrTypeDecl typeDecl)
    {
        TypeDeclarations.Add(typeDecl);
    }
}

/// <summary>
/// A type declaration in the IR. Preserves high-level structure (SIL-style)
/// so the code generator knows whether to emit a class, struct, or enum.
/// </summary>
public class IrTypeDecl
{
    /// <summary>
    /// The fully qualified name (e.g. "TestModule.Foo").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The kind of this type declaration.
    /// </summary>
    public IrTypeDeclKind DeclKind { get; }

    /// <summary>
    /// The fields/properties of this type.
    /// </summary>
    public List<IrFieldDecl> Fields { get; } = new();

    /// <summary>
    /// The base type, if any (for classes with inheritance).
    /// </summary>
    public IrType? BaseType { get; set; }

    /// <summary>
    /// Contracts (interfaces) this type conforms to.
    /// </summary>
    public List<IrType> Contracts { get; } = new();

    public IrTypeDecl(string name, IrTypeDeclKind kind)
    {
        Name = name;
        DeclKind = kind;
    }
}

public enum IrTypeDeclKind
{
    Class,
    Struct,
    Enum
}

/// <summary>
/// A field/property declaration within a type.
/// </summary>
public class IrFieldDecl
{
    public string Name { get; }
    public IrType Type { get; }
    public bool IsMutable { get; }

    public IrFieldDecl(string name, IrType type, bool isMutable)
    {
        Name = name;
        Type = type;
        IsMutable = isMutable;
    }
}
