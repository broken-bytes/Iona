using Tests.Helpers;

namespace Tests;

/// <summary>
/// Tests that the compiler correctly detects and reports errors.
/// </summary>
public class ErrorDetectionTests
{
    [Fact]
    public void UndefinedReturnType_ProducesError()
    {
        var source = @"
module TestModule

public class Foo {
    public fn bad() -> NonExistentType {
        return 1
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        // The return type is undefined, should produce an error
        Assert.NotEmpty(errors.Errors);
    }

    [Fact]
    public void UndefinedIdentifierInExpression_ProducesError()
    {
        var source = @"
module TestModule

public class Foo {
    public fn bad() -> Int32 {
        return unknown_var
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e => e.Message.Contains("unknown_var"));
    }

    [Fact]
    public void ValidProgram_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
    public var x = 10

    public fn get_x() -> Int32 {
        return x
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
    }
}
