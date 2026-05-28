//|--- CodegenRunner.cs ----------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using System.Diagnostics;

namespace Tests.Helpers;

public static class CodegenRunner
{
    private static readonly Lazy<string> RepoRoot = new(FindRepoRoot);

    public sealed record Result(int ExitCode, string Stdout, string Stderr, string CompileStderr);

    public static Result CompileAndRun(
        string fixtureRelativeToRepo,
        string? stdin = null,
        TimeSpan? timeout = null)
    {
        var root = RepoRoot.Value;
        var ionaExe = Path.Combine(root, "Toolchain", "Iona", "bin", "Debug", "net10.0", "Iona");
        var stdlibDir = Path.Combine(root, "SDK", "StandardLibrary", "bin", "Debug", "net10.0");
        var fixturePath = Path.Combine(root, fixtureRelativeToRepo);

        if (!File.Exists(ionaExe))
        {
            throw new FileNotFoundException(
                $"Iona CLI not built at {ionaExe}. Build the solution first (`dotnet build Iona.sln`).");
        }
        if (!File.Exists(fixturePath))
        {
            throw new FileNotFoundException($"Fixture not found: {fixturePath}");
        }
        if (!File.Exists(Path.Combine(stdlibDir, "Iona.Builtins.dll")))
        {
            throw new FileNotFoundException(
                $"Iona.Builtins.dll missing under {stdlibDir}. Build the StandardLibrary first.");
        }

        var tmpDir = Path.Combine(Path.GetTempPath(), "iona-codegen-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var assemblyName = Path.GetFileNameWithoutExtension(fixturePath) + "Test";
            var outputDll = Path.Combine(tmpDir, assemblyName + ".dll");

            var compile = Run(
                ionaExe,
                new[] { fixturePath, "-t", "exe", "-o", outputDll, "-a", stdlibDir, "-r", "Iona.Builtins" },
                stdin: null,
                timeout: timeout ?? TimeSpan.FromSeconds(30));

            if (compile.exit != 0)
            {
                return new Result(compile.exit, "", "", compile.stdout + compile.stderr);
            }

            var run = Run(
                "dotnet",
                new[] { outputDll },
                stdin: stdin,
                timeout: timeout ?? TimeSpan.FromSeconds(30));

            return new Result(run.exit, run.stdout, run.stderr, "");
        }
        finally
        {
            try { Directory.Delete(tmpDir, recursive: true); } catch { /* best-effort */ }
        }
    }

    private static (int exit, string stdout, string stderr) Run(
        string fileName, string[] args, string? stdin, TimeSpan timeout)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = stdin != null,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var proc = Process.Start(psi)!;
        if (stdin != null)
        {
            proc.StandardInput.Write(stdin);
            proc.StandardInput.Close();
        }

        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();

        if (!proc.WaitForExit((int)timeout.TotalMilliseconds))
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException($"`{fileName}` did not exit within {timeout}");
        }
        // Drain after WaitForExit so async reads aren't truncated.
        proc.WaitForExit();

        return (proc.ExitCode, stdoutTask.GetAwaiter().GetResult(), stderrTask.GetAwaiter().GetResult());
    }

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
        throw new InvalidOperationException(
            $"Could not locate Iona.sln from {AppContext.BaseDirectory}");
    }

    public static string[] Lines(string text) =>
        text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
}
