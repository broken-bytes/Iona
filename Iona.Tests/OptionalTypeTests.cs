using Tests.Helpers;

namespace Tests;

public class OptionalTypeTests
{
    [Fact]
    public void OptionalPropertyDeclaration_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
    public let name: String? = ""hello""

    public init() {}
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void OptionalReturnType_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
    public init() {}

    public fn maybeName() -> String? {
        return ""hello""
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void OptionalParameter_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
    public init() {}

    public fn greet(name: String?) -> String {
        return ""hello""
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void IUOPropertyDeclaration_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
    public let name: String! = ""hello""

    public init() {}
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }
}
