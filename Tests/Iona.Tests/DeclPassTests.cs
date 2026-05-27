//|--- DeclPassTests.cs ----------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using Symbols.Symbols;
using Tests.Helpers;

namespace Tests;

public class DeclPassTests
{
    [Fact]
    public void Module_RegistersModuleSymbol()
    {
        var source = @"
module TestModule
";
        var (errors, table) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
        var module = table.Modules.FirstOrDefault(m => m.Name == "TestModule");
        Assert.NotNull(module);
    }

    [Fact]
    public void Class_RegistersTypeSymbol()
    {
        var source = @"
module TestModule

public class Foo {
}
";
        var (errors, table) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
        var module = table.Modules.First(m => m.Name == "TestModule");
        var fooType = module.Symbols.OfType<TypeSymbol>().FirstOrDefault(t => t.Name == "Foo");
        Assert.NotNull(fooType);
        Assert.Equal(TypeKind.Class, fooType.TypeKind);
    }

    [Fact]
    public void Struct_RegistersTypeSymbol()
    {
        var source = @"
module TestModule

public struct Bar {
}
";
        var (errors, table) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
        var module = table.Modules.First(m => m.Name == "TestModule");
        var barType = module.Symbols.OfType<TypeSymbol>().FirstOrDefault(t => t.Name == "Bar");
        Assert.NotNull(barType);
        Assert.Equal(TypeKind.Struct, barType.TypeKind);
    }

    [Fact]
    public void Function_InsideClass_RegistersFuncSymbol()
    {
        var source = @"
module TestModule

public class Foo {
    public fn bar() -> Int32 {
        return 1
    }
}
";
        var (errors, table) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
        var module = table.Modules.First(m => m.Name == "TestModule");
        var fooType = module.Symbols.OfType<TypeSymbol>().First(t => t.Name == "Foo");
        var barFunc = fooType.Symbols.OfType<FuncSymbol>().FirstOrDefault(f => f.Name == "bar");
        Assert.NotNull(barFunc);
    }

    [Fact]
    public void Property_InsideClass_RegistersPropertySymbol()
    {
        var source = @"
module TestModule

public class Foo {
    public var x = 10
}
";
        var (errors, table) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
        var module = table.Modules.First(m => m.Name == "TestModule");
        var fooType = module.Symbols.OfType<TypeSymbol>().First(t => t.Name == "Foo");
        var xProp = fooType.Symbols.OfType<PropertySymbol>().FirstOrDefault(p => p.Name == "x");
        Assert.NotNull(xProp);
    }

    [Fact]
    public void Function_WithParameters_HasParameterSymbols()
    {
        var source = @"
module TestModule

public class Foo {
    public fn add(a: Int32, b: Int32) -> Int32 {
        return 1
    }
}
";
        var (errors, table) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
        var module = table.Modules.First(m => m.Name == "TestModule");
        var fooType = module.Symbols.OfType<TypeSymbol>().First(t => t.Name == "Foo");
        var addFunc = fooType.Symbols.OfType<FuncSymbol>().First(f => f.Name == "add");

        // Function should contain parameter symbols (may also contain variables from body)
        var parameters = addFunc.Symbols.OfType<ParameterSymbol>().ToList();
        Assert.True(parameters.Count >= 2, $"Expected at least 2 parameters, got {parameters.Count}");
        Assert.Contains(parameters, p => p.Name == "a");
        Assert.Contains(parameters, p => p.Name == "b");
    }
}
