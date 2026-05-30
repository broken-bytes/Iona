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

open public class Animal {
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

open public class Base {
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

open public class Base {
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

    [Fact]
    public void InheritingFromNonOpenClass_ProducesError()
    {
        var source = @"
module TestModule

public class Base {
    public init() {}
}

public class Child : Base {
    public init() {}
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e => e.Code == "C0031");
    }

    [Fact]
    public void OverrideOfOpenMethod_NoErrors()
    {
        var source = @"
module TestModule

open public class Base {
    public init() {}

    public open fn greet() -> String {
        return ""...""
    }
}

public class Child : Base {
    public init() {}

    public override fn greet() -> String {
        return ""hi""
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void OverrideOfNonOpenMethod_ProducesError()
    {
        var source = @"
module TestModule

open public class Base {
    public init() {}

    public fn greet() -> String {
        return ""...""
    }
}

public class Child : Base {
    public init() {}

    public override fn greet() -> String {
        return ""hi""
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e => e.Code == "C0032");
    }

    [Fact]
    public void ShadowingInheritedMethodWithoutOverride_ProducesError()
    {
        var source = @"
module TestModule

open public class Base {
    public init() {}

    public open fn greet() -> String {
        return ""...""
    }
}

public class Child : Base {
    public init() {}

    public fn greet() -> String {
        return ""hi""
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e => e.Code == "C0034");
    }

    [Fact]
    public void OverrideWithNoBaseMember_ProducesError()
    {
        var source = @"
module TestModule

open public class Base {
    public init() {}
}

public class Child : Base {
    public init() {}

    public override fn doesNotExist() -> String {
        return ""hi""
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e => e.Code == "C0033");
    }

    [Fact]
    public void GenericConstraint_ViolatedAtCallSite_ProducesC0035()
    {
        var source = @"
module TestModule

contract Numeric { }

#over<T> where T: Numeric
public class Box {
    public init() {}
}

public class App {
    public fn run() -> Int32 {
        let b = Box<String>()
        return 0
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e => e.Code == "C0035");
    }

    [Fact]
    public void GenericConstraint_SatisfiedByContractImpl_NoErrors()
    {
        var source = @"
module TestModule

public contract Numeric { }

public class Mass : Numeric {
    public init() {}
}

#over<T> where T: Numeric
public class Box {
    public init() {}
}

public class App {
    public fn run() -> Int32 {
        let b = Box<Mass>()
        return 0
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void GenericConstraint_MultipleBoundsAllSatisfied_NoErrors()
    {
        var source = @"
module TestModule

public contract Numeric { }
public contract Comparable { }

public class Mass : Numeric, Comparable {
    public init() {}
}

#over<T> where T: Numeric & Comparable
public class Box {
    public init() {}
}

public class App {
    public fn run() -> Int32 {
        let b = Box<Mass>()
        return 0
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void GenericConstraint_MultipleBoundsOneViolated_ProducesC0035()
    {
        var source = @"
module TestModule

public contract Numeric { }
public contract Comparable { }

// `OnlyNumeric` satisfies Numeric but NOT Comparable.
public class OnlyNumeric : Numeric {
    public init() {}
}

#over<T> where T: Numeric & Comparable
public class Box {
    public init() {}
}

public class App {
    public fn run() -> Int32 {
        let b = Box<OnlyNumeric>()
        return 0
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e => e.Code == "C0035" && e.Message.Contains("Comparable"));
    }

    [Fact]
    public void GenericConstraint_MixedBoundsAcrossParams_NoErrors()
    {
        var source = @"
module TestModule

public contract Numeric { }
public contract Comparable { }
public contract Clone { }

public class N : Numeric, Comparable { public init() {} }
public class C : Clone { public init() {} }

#over<T, U>
where T: Numeric & Comparable, U: Clone
public class Pair {
    public init() {}
}

public class App {
    public fn run() -> Int32 {
        let p = Pair<N, C>()
        return 0
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void GenericConstraint_SiblingParam_AcceptsSubtype()
    {
        var source = @"
module TestModule

open public class Animal { public init() {} }
public class Dog : Animal { public init() {} }

#over<Sub, Parent>
where Sub: Parent
public class Wrapper {
    public init() {}
}

public class App {
    public fn run() -> Int32 {
        let w = Wrapper<Dog, Animal>()
        return 0
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void GenericConstraint_SiblingParam_RejectsUnrelatedType()
    {
        var source = @"
module TestModule

open public class Animal { public init() {} }
public class Cat { public init() {} }

#over<Sub, Parent>
where Sub: Parent
public class Wrapper {
    public init() {}
}

public class App {
    public fn run() -> Int32 {
        let w = Wrapper<Cat, Animal>()
        return 0
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e => e.Code == "C0035" && e.Message.Contains("Animal"));
    }
}
