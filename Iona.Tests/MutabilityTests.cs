using Tests.Helpers;

namespace Tests;

public class MutabilityTests
{
    [Fact]
    public void LetVariable_CannotBeReassigned()
    {
        var source = @"
module TestModule

public class Foo {
    public fn test() -> Int32 {
        let x = 10
        x = 20
        return x
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e =>
            e.Code == "C0018" &&
            e.Message.Contains("x"));
    }

    [Fact]
    public void VarVariable_CanBeReassigned()
    {
        var source = @"
module TestModule

public class Foo {
    public fn test() -> Int32 {
        var x = 10
        x = 20
        return x
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.DoesNotContain(errors.Errors, e =>
            e.Code == "C0018");
    }
}
