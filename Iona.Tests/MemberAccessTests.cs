using Tests.Helpers;

namespace Tests;

public class MemberAccessTests
{
    [Fact]
    public void PropertyAccess_OnInstance_NoErrors()
    {
        var source = @"
module TestModule

public class Person {
    public let name: String = ""John""

    public init() {}
}

public class App {
    public init() {}

    public fn getName() -> String {
        var p = Person()
        return p.name
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void MethodCall_OnInstance_NoErrors()
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

    [Fact]
    public void ChainedPropertyAccess_NoErrors()
    {
        var source = @"
module TestModule

public class Inner {
    public let value: Int32 = 42

    public init() {}
}

public class Outer {
    public let inner: Inner = Inner()

    public init() {}
}

public class App {
    public init() {}

    public fn getValue() -> Int32 {
        var o = Outer()
        return o.inner.value
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void NonexistentProperty_ProducesError()
    {
        var source = @"
module TestModule

public class Foo {
    public init() {}
}

public class App {
    public init() {}

    public fn run() -> Int32 {
        var f = Foo()
        return f.missing
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e =>
            e.Code == "C0006" &&
            e.Message.Contains("missing"));
    }

    [Fact]
    public void SelfPropertyAccess_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
    public let name: String = ""hello""

    public init() {}

    public fn getName() -> String {
        return name
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void MethodCallOnStruct_NoErrors()
    {
        var source = @"
module TestModule

public struct Counter {
    public let count: Int32 = 0

    public init() {}

    public fn getCount() -> Int32 {
        return count
    }
}

public class App {
    public init() {}

    public fn run() -> Int32 {
        var c = Counter()
        return c.getCount()
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }
}
