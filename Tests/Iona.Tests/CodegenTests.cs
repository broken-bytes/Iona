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
    public void FirstClassFunctions_RefAndPassAndInvoke_RuntimeMatches()
    {
        var result = CodegenRunner.CompileAndRun("Tests/fixtures/iona/codegen_first_class_funcs.iona");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(new[] { "10", "14" }, CodegenRunner.Lines(result.Stdout));
    }

    [Fact]
    public void Lambdas_TypedUntypedCapturingImplicit_RuntimeMatches()
    {
        var result = CodegenRunner.CompileAndRun("Tests/fixtures/iona/codegen_lambdas.iona");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(new[] { "12", "20", "12", "42" }, CodegenRunner.Lines(result.Stdout));
    }

    [Fact]
    public void CollectionLiterals_ListAndMap_BuildAtRuntime()
    {
        var result = CodegenRunner.CompileAndRun("Tests/fixtures/iona/codegen_collections.iona");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(new[] { "made it" }, CodegenRunner.Lines(result.Stdout));
    }

    [Fact]
    public void OverDirective_GenericTypesEmitWithCecilGenericParams()
    {
        var result = CodegenRunner.CompileAndRun("Tests/fixtures/iona/codegen_over_generics.iona");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(new[] { "ok" }, CodegenRunner.Lines(result.Stdout));
    }

    [Fact]
    public void OverDirective_GenericMethodsMonomorphiseAtCallSite()
    {
        var result = CodegenRunner.CompileAndRun("Tests/fixtures/iona/codegen_over_generic_methods.iona");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(new[] { "42", "hi" }, CodegenRunner.Lines(result.Stdout));
    }

    [Fact]
    public void OverDirective_ConstraintsReachCecilMetadata()
    {
        // CompileAndRun deletes the temp output, so we re-do the produce step here and read
        // metadata before the run cleans up. We hijack the runner just to validate the run
        // succeeds; the assertions go against the assembly we keep alongside.
        var runResult = CodegenRunner.CompileAndRun(
            "Tests/fixtures/iona/codegen_constraints_metadata.iona");
        Assert.Equal(0, runResult.ExitCode);
        Assert.Equal(new[] { "ok" }, CodegenRunner.Lines(runResult.Stdout));

        // Compile a second time into a dedicated path we can inspect.
        var tmp = Path.Combine(Path.GetTempPath(), "iona-meta-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            var outDll = Path.Combine(tmp, "Inspected.dll");
            var psi = new System.Diagnostics.ProcessStartInfo(
                FindIonaCli(),
                "")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = FindRepoRoot()
            };
            psi.ArgumentList.Add(Path.Combine(FindRepoRoot(), "Tests/fixtures/iona/codegen_constraints_metadata.iona"));
            psi.ArgumentList.Add("-t");
            psi.ArgumentList.Add("exe");
            psi.ArgumentList.Add("-o");
            psi.ArgumentList.Add(outDll);
            psi.ArgumentList.Add("-a");
            psi.ArgumentList.Add(Path.Combine(FindRepoRoot(), "SDK/StandardLibrary/bin/Debug/net10.0"));
            psi.ArgumentList.Add("-r");
            psi.ArgumentList.Add("Iona.Builtins");
            using var proc = System.Diagnostics.Process.Start(psi)!;
            proc.WaitForExit(30_000);

            var entries = CecilInspector.ListGenericParamConstraints(outDll);
            Assert.Contains("App.Box`1.T=App.Numeric,App.Comparable", entries);
        }
        finally
        {
            try { Directory.Delete(tmp, recursive: true); } catch { }
        }
    }

    private static string FindIonaCli()
        => Path.Combine(FindRepoRoot(), "Toolchain/Iona/bin/Debug/net10.0/Iona");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Iona.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate Iona.sln");
    }

    [Fact]
    public void ExitCode_PropagatedFromMain()
    {
        var result = CodegenRunner.CompileAndRun("Tests/fixtures/iona/codegen_exit_code.iona");

        Assert.Equal(42, result.ExitCode);
        Assert.Equal("", result.Stdout.Trim());
    }
}
