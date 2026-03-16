using AST.Nodes;
using Symbols.Symbols;
using Tests.Helpers;

namespace Tests;

/// <summary>
/// End-to-end integration tests that exercise the full Lex -> Parse -> Typeck pipeline
/// on realistic Iona programs.
/// </summary>
public class IntegrationTests
{
    [Fact]
    public void HelloWorld_ClassWithProperties_NoErrors()
    {
        var source = @"
module Hello

public class Greeter {
    public var x = 10
    public var y = 20

    public fn greet() -> Int32 {
        var sum = x + y
        return sum
    }

    public fn loop_test() -> Int32 {
        var total = 0
        for i in 0...4 {
            total = total + i
        }
        return total
    }
}
";
        var (errors, table) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);

        // Verify the full symbol hierarchy was built
        var module = table.Modules.First(m => m.Name == "Hello");
        var greeterType = module.Symbols.OfType<TypeSymbol>().First(t => t.Name == "Greeter");

        // Properties
        var xProp = greeterType.Symbols.OfType<PropertySymbol>().FirstOrDefault(p => p.Name == "x");
        var yProp = greeterType.Symbols.OfType<PropertySymbol>().FirstOrDefault(p => p.Name == "y");
        Assert.NotNull(xProp);
        Assert.NotNull(yProp);

        // Functions
        var greetFunc = greeterType.Symbols.OfType<FuncSymbol>().FirstOrDefault(f => f.Name == "greet");
        var loopFunc = greeterType.Symbols.OfType<FuncSymbol>().FirstOrDefault(f => f.Name == "loop_test");
        Assert.NotNull(greetFunc);
        Assert.NotNull(loopFunc);

        // Variables registered under functions
        var sumVar = greetFunc.Symbols.OfType<VariableSymbol>().FirstOrDefault(v => v.Name == "sum");
        Assert.NotNull(sumVar);

        var totalVar = loopFunc.Symbols.OfType<VariableSymbol>().FirstOrDefault(v => v.Name == "total");
        Assert.NotNull(totalVar);
    }

    [Fact]
    public void EmptyClass_NoErrors()
    {
        var source = @"
module TestModule

public class Empty {
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void ClassWithMultipleMethods_NoParams_NoErrors()
    {
        var source = @"
module TestModule

public class Calculator {
    public var value = 0

    public fn get_value() -> Int32 {
        return value
    }

    public mut fn reset() -> Int32 {
        value = 0
        return value
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void PropertyTypeInference_IntAndString()
    {
        var source = @"
module TestModule

public class Foo {
    public var count = 0
    public var name = ""test""
}
";
        var (errors, table) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);

        var module = table.Modules.First(m => m.Name == "TestModule");
        var fooType = module.Symbols.OfType<TypeSymbol>().First(t => t.Name == "Foo");
        var props = fooType.Symbols.OfType<PropertySymbol>().ToList();

        var countProp = props.First(p => p.Name == "count");
        var nameProp = props.First(p => p.Name == "name");

        Assert.Equal("Int32", countProp.Type.Name);
        Assert.Equal("String", nameProp.Type.Name);
    }

    [Fact]
    public void FunctionReturnType_ResolvedInAST()
    {
        var source = @"
module TestModule

public class Foo {
    public fn get_number() -> Int32 {
        return 42
    }
}
";
        var (errors, _, ast) = TestHelpers.RunTypeckWithAst(source);

        Assert.Empty(errors.Errors);

        var moduleNode = ast.Children.OfType<ModuleNode>().First();
        var classNode = moduleNode.Children.OfType<ClassNode>().First();
        var funcNode = classNode.Body!.Children.OfType<FuncNode>().First();

        Assert.NotNull(funcNode.ReturnType);
        Assert.Contains("Int32", funcNode.ReturnType.FullyQualifiedName);
    }

    [Fact]
    public void ComplexExpression_NestedBinaryOps_Resolves()
    {
        var source = @"
module TestModule

public class Math {
    public var a = 1
    public var b = 2
    public var c = 3

    public fn compute() -> Int32 {
        var result = a + b + c
        return result
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
    }
}
