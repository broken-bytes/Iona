//|--- AccessLevelTests.cs -------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using Tests.Helpers;

namespace Tests;

public class AccessLevelTests
{
    [Fact]
    public void PublicPropertyAccess_NoErrors()
    {
        var source = @"
module TestModule

public class Person {
    public let name: String = ""John""

    public init() {}
}

public class Printer {
    public init() {}

    public fn printName() -> String {
        var p = Person()
        return p.name
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void PublicMethodAccess_NoErrors()
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
    public void PublicPropertyOnStruct_NoErrors()
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

    public fn getX() -> Int32 {
        var p = Point()
        return p.x
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }
}
