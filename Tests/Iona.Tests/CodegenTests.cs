//|--- CodegenTests.cs -----------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using Tests.Helpers;

namespace Tests;

public class CodegenTests
{
    [Fact]
    public void SelfAssign_PersistsThroughInit_RuntimeMatches()
    {
        var result = CodegenRunner.CompileAndRun("Tests/fixtures/iona/self_assign.iona");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(new[] { "A_OK", "B_OK" }, CodegenRunner.Lines(result.Stdout));
    }

    [Fact]
    public void StringInterpolation_AllForms_RuntimeMatches()
    {
        var result = CodegenRunner.CompileAndRun("Tests/fixtures/iona/interp.iona");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            new[]
            {
                "hello world",
                "n = 42",
                "no interp here, just $name literal",
                "mix: world is 42",
                "escape: ${name} stays literal, value was world",
                "backslash: a\\b",
                "nested: inner world",
                "nested-plain: plain"
            },
            CodegenRunner.Lines(result.Stdout));
    }

    [Fact]
    public void Inheritance_VirtualDispatchAndSuper_RuntimeMatches()
    {
        var result = CodegenRunner.CompileAndRun("Tests/fixtures/iona/inheritance.iona");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            new[]
            {
                "Animal says woof (parent says: ...)",
                "Animal says meow",
                "Animal says ..."
            },
            CodegenRunner.Lines(result.Stdout));
    }

    [Fact]
    public void RecordEquality_DefaultStructuralEquality_RuntimeMatches()
    {
        var result = CodegenRunner.CompileAndRun("Tests/fixtures/iona/codegen_record_equality.iona");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(new[] { "equal", "not equal" }, CodegenRunner.Lines(result.Stdout));
    }

    [Fact]
    public void Arithmetic_IntDoubleOps_RuntimeMatches()
    {
        var result = CodegenRunner.CompileAndRun("Tests/fixtures/iona/codegen_arithmetic.iona");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(new[] { "7", "12", "1", "8.5" }, CodegenRunner.Lines(result.Stdout));
    }

    [Fact]
    public void ControlFlow_IfForWhile_RuntimeMatches()
    {
        var result = CodegenRunner.CompileAndRun("Tests/fixtures/iona/codegen_control_flow.iona");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            new[] { "if-branch", "0", "1", "2", "0", "1", "2", "3" },
            CodegenRunner.Lines(result.Stdout));
    }

    [Fact]
    public void ExitCode_PropagatedFromMain()
    {
        var result = CodegenRunner.CompileAndRun("Tests/fixtures/iona/codegen_exit_code.iona");

        Assert.Equal(42, result.ExitCode);
        Assert.Equal("", result.Stdout.Trim());
    }
}
