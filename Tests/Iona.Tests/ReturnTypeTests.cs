//|--- ReturnTypeTests.cs --------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using Tests.Helpers;

namespace Tests;

public class ReturnTypeTests
{
    [Fact]
    public void CorrectReturnType_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
    public init() {}

    public fn getNumber() -> Int32 {
        return 42
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void WrongReturnType_ProducesError()
    {
        var source = @"
module TestModule

public class Foo {
    public init() {}

    public fn getNumber() -> Int32 {
        return ""not a number""
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e =>
            e.Code == "C0026");
    }

    [Fact]
    public void ReturnStringFromIntFunc_ProducesError()
    {
        var source = @"
module TestModule

public class Foo {
    public init() {}

    public fn getMessage() -> String {
        return 123
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e =>
            e.Code == "C0026");
    }

    [Fact]
    public void VoidFunction_NoReturnNeeded_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
    public var count: Int32 = 0

    public init() {}

    public mut fn increment() {
        count = count + 1
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void ReturnPropertyValue_CorrectType_NoErrors()
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
    public void ReturnLocalVariable_CorrectType_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
    public init() {}

    public fn compute() -> Int32 {
        var result = 42
        return result
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }
}
