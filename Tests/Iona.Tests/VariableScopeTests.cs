//|--- VariableScopeTests.cs -----------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Nodes;
using Symbols.Symbols;
using Tests.Helpers;

namespace Tests;

public class VariableScopeTests
{
    [Fact]
    public void Variable_WithLiteralValue_TypeInferred()
    {
        var source = @"
module TestModule

public class Foo {
    public fn bar() -> Int32 {
        var x = 42
        return x
    }
}
";
        var (errors, table, ast) = TestHelpers.RunTypeckWithAst(source);

        Assert.Empty(errors.Errors);

        // The variable should be registered under the function scope
        var module = table.Modules.First(m => m.Name == "TestModule");
        var fooType = module.Symbols.OfType<TypeSymbol>().First(t => t.Name == "Foo");
        var barFunc = fooType.Symbols.OfType<FuncSymbol>().First(f => f.Name == "bar");
        var xVar = barFunc.Symbols.OfType<VariableSymbol>().FirstOrDefault(v => v.Name == "x");

        Assert.NotNull(xVar);
        Assert.Equal("Int32", xVar.Type.Name);
    }

    [Fact]
    public void Variable_UsableInSubsequentReturn()
    {
        var source = @"
module TestModule

public class Foo {
    public fn compute() -> Int32 {
        var result = 10
        return result
    }
}
";
        var (errors, _, ast) = TestHelpers.RunTypeckWithAst(source);

        Assert.Empty(errors.Errors);

        // Find the return node and verify it resolved
        var moduleNode = ast.Children.OfType<ModuleNode>().First();
        var classNode = moduleNode.Children.OfType<ClassNode>().First();
        var funcNode = classNode.Body!.Children.OfType<FuncNode>().First();
        var returnNode = funcNode.Body!.Children.OfType<ReturnNode>().First();

        // The return value (identifier "result") should have resolved
        Assert.NotNull(returnNode.Value);
        Assert.NotEqual(INode.ResolutionStatus.Failed, returnNode.Value.Status);
    }

    [Fact]
    public void Variable_UsedInBinaryExpression_Resolves()
    {
        var source = @"
module TestModule

public class Foo {
    public var x = 10
    public var y = 20

    public fn sum() -> Int32 {
        var result = x + y
        return result
    }
}
";
        var (errors, table) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);

        var module = table.Modules.First(m => m.Name == "TestModule");
        var fooType = module.Symbols.OfType<TypeSymbol>().First(t => t.Name == "Foo");
        var sumFunc = fooType.Symbols.OfType<FuncSymbol>().First(f => f.Name == "sum");
        var resultVar = sumFunc.Symbols.OfType<VariableSymbol>().FirstOrDefault(v => v.Name == "result");

        Assert.NotNull(resultVar);
        Assert.Equal("Int32", resultVar.Type.Name);
    }

    [Fact]
    public void MultipleVariables_AllRegistered()
    {
        var source = @"
module TestModule

public class Foo {
    public fn test() -> Int32 {
        var a = 1
        var b = 2
        var c = 3
        return c
    }
}
";
        var (errors, table) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);

        var module = table.Modules.First(m => m.Name == "TestModule");
        var fooType = module.Symbols.OfType<TypeSymbol>().First(t => t.Name == "Foo");
        var testFunc = fooType.Symbols.OfType<FuncSymbol>().First(f => f.Name == "test");
        var vars = testFunc.Symbols.OfType<VariableSymbol>().Select(v => v.Name).ToList();

        Assert.Contains("a", vars);
        Assert.Contains("b", vars);
        Assert.Contains("c", vars);
    }
}
