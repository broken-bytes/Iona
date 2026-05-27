//|--- InheritanceTests.cs -------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using Tests.Helpers;

namespace Tests;

public class InheritanceTests
{
    [Fact]
    public void ClassInheritsFromClass_NoErrors()
    {
        var source = @"
module TestModule

public class Animal {
    public let name: String = ""animal""

    public init() {}

    public fn speak() -> String {
        return ""...""
    }
}

public class Dog : Animal {
    public init() {}

    public fn bark() -> String {
        return ""woof""
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void ClassWithContractAndBaseClass_NoErrors()
    {
        var source = @"
module TestModule

public contract Greetable {
    fn greet() -> String
}

public class Base {
    public init() {}
}

public class Child : Base, Greetable {
    public init() {}

    public fn greet() -> String {
        return ""hello""
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void StructCannotInheritFromClass_ProducesError()
    {
        var source = @"
module TestModule

public class Base {
    public init() {}
}

public struct Child : Base {
    public init() {}
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e =>
            e.Code == "C0028" &&
            e.Message.Contains("Child") &&
            e.Message.Contains("Base"));
    }

    [Fact]
    public void RecordCannotInheritFromClass_ProducesError()
    {
        var source = @"
module TestModule

public class Base {
    public init() {}
}

public record Child : Base {
    public let name: String = ""test""
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e =>
            e.Code == "C0028" &&
            e.Message.Contains("Child") &&
            e.Message.Contains("Base"));
    }

    [Fact]
    public void AccessBaseMethodOnInstance_NoErrors()
    {
        var source = @"
module TestModule

public class Base {
    public init() {}

    public fn baseMethod() -> String {
        return ""from base""
    }
}

public class Child : Base {
    public init() {}
}

public class App {
    public init() {}

    public fn run() -> String {
        var c = Child()
        return c.baseMethod()
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }
}
