//|--- RecordTests.cs ------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using Tests.Helpers;

namespace Tests;

public class RecordTests
{
    [Fact]
    public void Record_WithLetProperties_NoErrors()
    {
        var source = @"
module TestModule

public record Person {
    public let name: String = ""John""
    public let age: Int32 = 30
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void Record_WithVarProperty_ProducesError()
    {
        var source = @"
module TestModule

public record Person {
    public var name: String = ""John""
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e =>
            e.Code == "C0023" &&
            e.Message.Contains("name"));
    }

    [Fact]
    public void Record_WithMutatingFunc_ProducesError()
    {
        var source = @"
module TestModule

public record Person {
    public let name: String = ""John""

    public mut fn setName(newName: String) {
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e =>
            e.Code == "C0024" &&
            e.Message.Contains("setName"));
    }

    [Fact]
    public void Record_WithImmutableFunc_NoErrors()
    {
        var source = @"
module TestModule

public record Person {
    public let name: String = ""John""

    public fn getName() -> String {
        return name
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void Record_CannotInheritFromClass_ProducesError()
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
    public void Record_ImplementsContract_NoErrors()
    {
        var source = @"
module TestModule

public contract Named {
    fn getName() -> String
}

public record Person : Named {
    public let name: String = ""John""

    public fn getName() -> String {
        return name
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }
}
