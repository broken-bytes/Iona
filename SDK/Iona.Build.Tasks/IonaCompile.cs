//|--- IonaCompile.cs ------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Iona.Build.Tasks
{
    public class IonaCompile : Task
    {
        [Required] public ITaskItem[] Sources { get; set; } = Array.Empty<ITaskItem>();

        public ITaskItem[] References { get; set; } = Array.Empty<ITaskItem>();

        [Required] public string OutputAssembly { get; set; } = "";

        public string OutputType { get; set; } = "dll";

        public string TargetFramework { get; set; } = "";

        [Required] public string IonaCompilerPath { get; set; } = "";

        public string DotnetHostPath { get; set; } = "";

        public override bool Execute()
        {
            if (Sources.Length == 0)
            {
                Log.LogError("IonaCompile: no .iona source files were provided.");
                return false;
            }
            if (!File.Exists(IonaCompilerPath))
            {
                Log.LogError($"IonaCompile: Iona compiler not found at '{IonaCompilerPath}'.");
                return false;
            }

            var outputFull = Path.GetFullPath(OutputAssembly);
            var workDir = Path.GetDirectoryName(outputFull) ?? ".";
            Directory.CreateDirectory(workDir);
            if (File.Exists(outputFull))
            {
                File.Delete(outputFull);
            }

            var args = BuildArguments(outputFull);
            Log.LogMessage(MessageImportance.High, $"IonaCompile: compiling {Sources.Length} file(s) -> {OutputAssembly}");

            var host = string.IsNullOrEmpty(DotnetHostPath) ? "dotnet" : DotnetHostPath;
            var psi = new ProcessStartInfo
            {
                FileName = host,
                Arguments = $"\"{IonaCompilerPath}\" {args}",
                WorkingDirectory = workDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using (var proc = new Process())
            {
                proc.StartInfo = psi;
                var stdout = new StringBuilder();
                var stderr = new StringBuilder();
                proc.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        stdout.AppendLine(e.Data);
                    }
                };
                proc.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        stderr.AppendLine(e.Data);
                    }
                };
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();

                if (stdout.Length > 0)
                {
                    Log.LogMessage(MessageImportance.Normal, stdout.ToString());
                }
                if (stderr.Length > 0)
                {
                    Log.LogMessage(MessageImportance.High, stderr.ToString());
                }

                if (proc.ExitCode != 0)
                {
                    Log.LogError($"IonaCompile: compiler exited with code {proc.ExitCode}.");
                    return false;
                }
            }

            if (!File.Exists(outputFull))
            {
                Log.LogError("IonaCompile: compilation did not produce an assembly (compile errors above?).");
                return false;
            }

            return !Log.HasLoggedErrors;
        }

        private string BuildArguments(string outputFull)
        {
            var sb = new StringBuilder();

            foreach (var src in Sources)
            {
                sb.Append('"').Append(Path.GetFullPath(src.ItemSpec)).Append("\" ");
            }

            var refs = new List<string>();
            foreach (var r in References)
            {
                var p = r.GetMetadata("FullPath");
                if (!string.IsNullOrEmpty(p))
                {
                    refs.Add(p);
                }
            }
            if (refs.Count > 0)
            {
                sb.Append("-r ");
                foreach (var p in refs)
                {
                    sb.Append('"').Append(p).Append("\" ");
                }
            }

            var fw = MapFramework(TargetFramework);
            if (!string.IsNullOrEmpty(fw))
            {
                sb.Append("-f \"").Append(fw).Append("\" ");
            }

            var target = string.Equals(OutputType, "exe", StringComparison.OrdinalIgnoreCase) ? "exe" : "dll";
            sb.Append("-t ").Append(target).Append(' ');

            sb.Append("-o \"").Append(outputFull).Append('"');

            return sb.ToString().Trim();
        }

        private static string MapFramework(string tfm)
        {
            if (string.IsNullOrEmpty(tfm))
            {
                return "";
            }
            if (tfm.StartsWith("netstandard2", StringComparison.OrdinalIgnoreCase))
            {
                return "netstandard2.0";
            }
            if (tfm.StartsWith("net4", StringComparison.OrdinalIgnoreCase))
            {
                return ".NET Framework 4";
            }
            
            return "";
        }
    }
}
