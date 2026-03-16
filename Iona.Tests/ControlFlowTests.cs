using Tests.Helpers;

namespace Tests;

public class ControlFlowTests
{
    [Fact]
    public void IfStatement_BoolCondition_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
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
    public void IfElse_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
    public var value: Int32 = 0

    public init() {}

    public fn getSign() -> Int32 {
        if value > 0 {
            return 1
        } else {
            return 0
        }
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void IfElseIf_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
    public var value: Int32 = 0

    public init() {}

    public fn classify() -> Int32 {
        if value > 0 {
            return 1
        } else if value < 0 {
            return 0
        } else {
            return 0
        }
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void WhileLoop_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
    public var count: Int32 = 10

    public init() {}

    public mut fn countdown() {
        while count > 0 {
            count = count - 1
        }
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void ForLoop_WithRange_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
    public fn addUp() -> Int32 {
        var sum = 0
        for i in 0...10 {
            sum = sum + i
        }
        return sum
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void NestedIfInLoop_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
    public fn process() -> Int32 {
        var count = 0
        for i in 0...10 {
            if i > 5 {
                count = count + 1
            }
        }
        return count
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);
        Assert.Empty(errors.Errors);
    }
}
