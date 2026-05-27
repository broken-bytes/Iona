//|--- StructTests.cs ------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using Tests.Helpers;

namespace Tests;

public class StructTests
{
    [Fact]
    public void BasicStruct_WithProperties_NoErrors()
    {
        var source = @"
module TestModule

public struct Point {
    public let x: Int32 = 0
    public let y: Int32 = 0

    public init() {}
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void Struct_WithMethod_NoErrors()
    {
        var source = @"
module TestModule

public struct Point {
    public let x: Int32 = 0
    public let y: Int32 = 0

    public init() {}

    public fn sum() -> Int32 {
        return x + y
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void Struct_WithMutatingMethod_NoErrors()
    {
        var source = @"
module TestModule

public struct Counter {
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
    public void Struct_CannotInheritFromClass_ProducesError()
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
    public void Struct_ImplementsContract_NoErrors()
    {
        var source = @"
module TestModule

public contract Describable {
    fn describe() -> String
}

public struct Point : Describable {
    public let x: Int32 = 0

    public init() {}

    public fn describe() -> String {
        return ""point""
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void Struct_MissingContractMember_ProducesError()
    {
        var source = @"
module TestModule

public contract Describable {
    fn describe() -> String
}

public struct Point : Describable {
    public let x: Int32 = 0

    public init() {}
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e =>
            e.Code == "C0027" &&
            e.Message.Contains("describe"));
    }
}
