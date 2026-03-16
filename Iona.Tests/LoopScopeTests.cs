using AST.Nodes;
using Symbols.Symbols;
using Tests.Helpers;

namespace Tests;

/// <summary>
/// Tests that for-loop iterator variables are correctly scoped to the loop body
/// and not accessible outside of it.
/// </summary>
public class LoopScopeTests
{
    [Fact]
    public void ForLoop_IteratorAccessibleInsideBody()
    {
        var source = @"
module TestModule

public class Foo {
    public fn loop_test() -> Int32 {
        var total = 0
        for i in 0...4 {
            total = total + i
        }
        return total
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void ForLoop_IteratorNotAccessibleOutsideBody()
    {
        var source = @"
module TestModule

public class Foo {
    public fn bad_loop() -> Int32 {
        for i in 0...4 {
            var x = i
        }
        return i
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        // Should produce an error: `i` is not defined outside the loop
        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e => e.Message.Contains("i"));
    }

    [Fact]
    public void ForLoop_UnderscoreIterator_NoRegistration()
    {
        var source = @"
module TestModule

public class Foo {
    public fn repeat() -> Int32 {
        var count = 0
        for _ in 0...3 {
            count = count + 1
        }
        return count
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void ForLoop_VariablesDeclaredInsideLoop_ScopeCorrectly()
    {
        var source = @"
module TestModule

public class Foo {
    public fn test() -> Int32 {
        var total = 0
        for i in 0...4 {
            var doubled = i + i
            total = total + doubled
        }
        return total
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void ForLoop_AssignmentWithIterator_Resolves()
    {
        var source = @"
module TestModule

public class Foo {
    public var x = 0

    public mut fn fill() -> Int32 {
        for i in 0...9 {
            x = i
        }
        return x
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void ForLoop_IteratorNotLeakedToSubsequentCode()
    {
        // Ensure the iterator from one loop doesn't leak into code after the loop
        var source = @"
module TestModule

public class Foo {
    public fn test() -> Int32 {
        for i in 0...4 {
            var x = i
        }
        var y = 10
        return y
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        // Should compile fine — y = 10 doesn't reference i
        Assert.Empty(errors.Errors);
    }
}
