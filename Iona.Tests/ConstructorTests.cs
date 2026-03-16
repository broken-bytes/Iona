using Tests.Helpers;

namespace Tests;

public class ConstructorTests
{
    [Fact]
    public void ParameterlessInit_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
    public init() {}
}

public class App {
    public init() {}

    public fn create() -> Int32 {
        var f = Foo()
        return 0
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void StructConstructor_NoErrors()
    {
        var source = @"
module TestModule

public struct Point {
    public let x: Int32 = 0
    public let y: Int32 = 0

    public init() {}
}

public class App {
    public init() {}

    public fn create() -> Int32 {
        var p = Point()
        return p.x
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void RecordWithProperties_NoErrors()
    {
        var source = @"
module TestModule

public record Config {
    public let name: String = ""default""
    public let version: Int32 = 1
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void ConstructAndAccessProperty_NoErrors()
    {
        var source = @"
module TestModule

public class Person {
    public let name: String = ""John""

    public init() {}
}

public class App {
    public init() {}

    public fn run() -> String {
        var p = Person()
        return p.name
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void ConstructAndCallMethod_NoErrors()
    {
        var source = @"
module TestModule

public class Greeter {
    public init() {}

    public fn greet() -> String {
        return ""hello""
    }
}

public class App {
    public init() {}

    public fn run() -> String {
        var g = Greeter()
        return g.greet()
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }
}
