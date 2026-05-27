//|--- ContractConformanceTests.cs -----------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using Tests.Helpers;

namespace Tests;

public class ContractConformanceTests
{
    [Fact]
    public void ClassImplementsAllContractMembers_NoErrors()
    {
        var source = @"
module TestModule

public contract Greetable {
    fn greet() -> String
}

public class Person : Greetable {
    public fn greet() -> String {
        return ""hello""
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void ClassMissingContractFunction_ProducesError()
    {
        var source = @"
module TestModule

public contract Greetable {
    fn greet() -> String
}

public class Person : Greetable {
    public init() {}
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e =>
            e.Code == "C0027" &&
            e.Message.Contains("Person") &&
            e.Message.Contains("greet") &&
            e.Message.Contains("Greetable"));
    }

    [Fact]
    public void ClassMissingContractProperty_ProducesError()
    {
        var source = @"
module TestModule

public contract Named {
    let name: String
}

public class Person : Named {
    public init() {}
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e =>
            e.Code == "C0027" &&
            e.Message.Contains("Person") &&
            e.Message.Contains("name") &&
            e.Message.Contains("Named"));
    }

    [Fact]
    public void ClassImplementsContractProperty_NoErrors()
    {
        var source = @"
module TestModule

public contract Named {
    let name: String
}

public class Person : Named {
    public let name: String = ""John""

    public init() {}
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void StructImplementsContract_NoErrors()
    {
        var source = @"
module TestModule

public contract Describable {
    fn describe() -> String
}

public struct Point : Describable {
    public let x: Int32 = 0
    public let y: Int32 = 0

    public fn describe() -> String {
        return ""point""
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void StructMissingContractFunction_ProducesError()
    {
        var source = @"
module TestModule

public contract Describable {
    fn describe() -> String
}

public struct Point : Describable {
    public let x: Int32 = 0
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e =>
            e.Code == "C0027" &&
            e.Message.Contains("Point") &&
            e.Message.Contains("describe"));
    }

    [Fact]
    public void ContractRefinement_MustImplementBothContracts()
    {
        var source = @"
module TestModule

public contract Named {
    fn getName() -> String
}

public contract Greetable : Named {
    fn greet() -> String
}

public class Person : Greetable {
    public fn greet() -> String {
        return ""hi""
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        // Should fail because Person doesn't implement getName from Named
        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e =>
            e.Code == "C0027" &&
            e.Message.Contains("getName"));
    }

    [Fact]
    public void ContractRefinement_AllMembersImplemented_NoErrors()
    {
        var source = @"
module TestModule

public contract Named {
    fn getName() -> String
}

public contract Greetable : Named {
    fn greet() -> String
}

public class Person : Greetable {
    public fn getName() -> String {
        return ""John""
    }

    public fn greet() -> String {
        return ""hi""
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void MultipleContracts_MustImplementAll()
    {
        var source = @"
module TestModule

public contract Named {
    fn getName() -> String
}

public contract Aged {
    fn getAge() -> Int32
}

public class Person : Named, Aged {
    public fn getName() -> String {
        return ""John""
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        // Missing getAge from Aged
        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e =>
            e.Code == "C0027" &&
            e.Message.Contains("getAge") &&
            e.Message.Contains("Aged"));
    }

    [Fact]
    public void MultipleContracts_AllImplemented_NoErrors()
    {
        var source = @"
module TestModule

public contract Named {
    fn getName() -> String
}

public contract Aged {
    fn getAge() -> Int32
}

public class Person : Named, Aged {
    public fn getName() -> String {
        return ""John""
    }

    public fn getAge() -> Int32 {
        return 30
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
    public void ContractWithNoMembers_AnyClassConforms()
    {
        var source = @"
module TestModule

public contract Marker {}

public class Foo : Marker {
    public init() {}
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
    }
}
