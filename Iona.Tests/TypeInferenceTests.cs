using Tests.Helpers;

namespace Tests;

public class TypeInferenceTests
{
    [Fact]
    public void InferIntFromLiteral_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
    public let value = 42

    public init() {}
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void InferStringFromLiteral_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
    public let name = ""hello""

    public init() {}
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void InferDoubleFromLiteral_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
    public let pi = 3.14

    public init() {}
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void InferTypeFromConstructor_NoErrors()
    {
        var source = @"
module TestModule

public class Bar {
    public init() {}
}

public class Foo {
    public init() {}

    public fn create() -> Int32 {
        var b = Bar()
        return 0
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void ExplicitTypeAnnotation_IntAndString_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
    public let count: Int32 = 10
    public let name: String = ""hello""

    public init() {}
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void LocalVariable_TypeInferred_UsableInReturn()
    {
        var source = @"
module TestModule

public class Foo {
    public init() {}

    public fn compute() -> Int32 {
        var x = 10
        var y = 20
        return x + y
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }
}
