//|--- IrModule.cs ---------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace IR;

public class IrModule
{
    public string Name { get; }

    public List<IrFunction> Functions { get; } = new();

    public List<IrTypeDecl> TypeDeclarations { get; } = new();

    public IrModule(string name)
    {
        Name = name;
    }

    public void AddFunction(IrFunction function)
    {
        Functions.Add(function);
    }

    public void AddTypeDeclaration(IrTypeDecl typeDecl)
    {
        TypeDeclarations.Add(typeDecl);
    }
}

public class IrTypeDecl
{
    public string Name { get; }

    public IrTypeDeclKind DeclKind { get; }

    public List<IrFieldDecl> Fields { get; } = new();

    public IrType? BaseType { get; set; }

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
