using Tests.Helpers;

namespace Tests;

public class OperatorTests
{
    [Fact]
    public void IntegerArithmetic_AllOps_NoErrors()
    {
        var source = @"
module TestModule

public class Math {
    public init() {}

    public fn compute() -> Int32 {
        var a = 10
        var b = 3
        var sum = a + b
        var diff = a - b
        var prod = a * b
        var quot = a / b
        return sum
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void ComparisonOperator_InCondition_NoErrors()
    {
        var source = @"
module TestModule

public class Checker {
    public var value: Int32 = 0

    public init() {}

    public mut fn check() {
        if value > 0 {
            value = 1
        }
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void StringConcatenation_NoErrors()
    {
        var source = @"
module TestModule

public class Greeter {
    public let name: String = ""World""

    public init() {}

    public fn greet() -> String {
        return ""Hello, "" + name
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void CompoundAssignment_NoErrors()
    {
        var source = @"
module TestModule

public class Counter {
    public var count: Int32 = 0

    public init() {}

    public mut fn add() {
        count = count + 10
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void EqualityComparison_InCondition_NoErrors()
    {
        var source = @"
module TestModule

public class Checker {
    public var value: Int32 = 0

    public init() {}

    public mut fn reset() {
        if value == 0 {
            value = 1
        }
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }
}
