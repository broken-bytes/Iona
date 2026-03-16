using AST.Nodes;
using AST.Types;
using Tests.Helpers;

namespace Tests;

/// <summary>
/// Tests that the ExpressionResolver correctly resolves expression types,
/// identifies errors for undefined names, and handles binary expressions.
/// </summary>
public class ExpressionResolverTests
{
    [Fact]
    public void IntegerLiteral_ResolvesToInt32()
    {
        var source = @"
module TestModule

public class Foo {
    public var x = 42
}
";
        var (errors, _, ast) = TestHelpers.RunTypeckWithAst(source);

        Assert.Empty(errors.Errors);

        var moduleNode = ast.Children.OfType<ModuleNode>().First();
        var classNode = moduleNode.Children.OfType<ClassNode>().First();
        var propNode = classNode.Body!.Children.OfType<PropertyNode>().First();

        Assert.NotNull(propNode.TypeNode);
        Assert.Contains("Int32", propNode.TypeNode.FullyQualifiedName);
    }

    [Fact]
    public void StringLiteral_ResolvesToString()
    {
        var source = @"
module TestModule

public class Foo {
    public var name = ""hello""
}
";
        var (errors, _, ast) = TestHelpers.RunTypeckWithAst(source);

        Assert.Empty(errors.Errors);

        var moduleNode = ast.Children.OfType<ModuleNode>().First();
        var classNode = moduleNode.Children.OfType<ClassNode>().First();
        var propNode = classNode.Body!.Children.OfType<PropertyNode>().First();

        Assert.NotNull(propNode.TypeNode);
        Assert.Contains("String", propNode.TypeNode.FullyQualifiedName);
    }

    [Fact]
    public void BinaryExpression_SameTypes_Resolves()
    {
        var source = @"
module TestModule

public class Foo {
    public var x = 10
    public var y = 20

    public fn add() -> Int32 {
        var sum = x + y
        return sum
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void Identifier_ResolvesToProperty_NoErrors()
    {
        var source = @"
module TestModule

public class Foo {
    public var x = 10

    public fn get_x() -> Int32 {
        return x
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        // The property x should be accessible in the function body
        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void UnknownIdentifier_ProducesError()
    {
        var source = @"
module TestModule

public class Foo {
    public fn bad() -> Int32 {
        return nonexistent
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.NotEmpty(errors.Errors);
        Assert.Contains(errors.Errors, e => e.Message.Contains("nonexistent"));
    }

    [Fact]
    public void ReturnStatement_ResolvesValueExpression()
    {
        var source = @"
module TestModule

public class Foo {
    public fn get_value() -> Int32 {
        return 42
    }
}
";
        var (errors, _, ast) = TestHelpers.RunTypeckWithAst(source);

        Assert.Empty(errors.Errors);

        var moduleNode = ast.Children.OfType<ModuleNode>().First();
        var classNode = moduleNode.Children.OfType<ClassNode>().First();
        var funcNode = classNode.Body!.Children.OfType<FuncNode>().First();
        var returnNode = funcNode.Body!.Children.OfType<ReturnNode>().First();

        Assert.NotNull(returnNode.Value);
        Assert.NotNull(returnNode.Value.ResultType);
        Assert.Contains("Int32", returnNode.Value.ResultType.FullyQualifiedName);
    }

    [Fact]
    public void Assignment_ToExistingVariable_Resolves()
    {
        var source = @"
module TestModule

public class Foo {
    public fn test() -> Int32 {
        var x = 0
        x = 42
        return x
    }
}
";
        var (errors, _) = TestHelpers.RunTypeck(source);

        Assert.Empty(errors.Errors);
    }

    [Fact]
    public void ArrayAccess_ParsesIntoArrayAccessNode()
    {
        var source = @"
module TestModule

public class Foo {
    public var items = 0

    public fn get_first() -> Int32 {
        var arr = items
        return arr[0]
    }
}
";
        var (_, _, ast) = TestHelpers.RunTypeckWithAst(source);

        var moduleNode = ast.Children.OfType<ModuleNode>().First();
        var classNode = moduleNode.Children.OfType<ClassNode>().First();
        var funcNode = classNode.Body!.Children.OfType<FuncNode>().First(f => f.Name == "get_first");
        var returnNode = funcNode.Body!.Children.OfType<ReturnNode>().First();

        // The return value should be an ArrayAccessNode
        Assert.IsType<ArrayAccessNode>(returnNode.Value);
        var arrayAccess = (ArrayAccessNode)returnNode.Value;

        // The array target should be the identifier "arr"
        Assert.IsType<IdentifierNode>(arrayAccess.Array);
        Assert.Equal("arr", ((IdentifierNode)arrayAccess.Array).Value);

        // The index should be the literal 0
        Assert.IsType<LiteralNode>(arrayAccess.Index);
        Assert.Equal("0", ((LiteralNode)arrayAccess.Index).Value);
    }
}
