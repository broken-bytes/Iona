//|--- Assembly.cs ---------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Nodes;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Mono.Cecil;
using Symbols;

namespace Generator
{
    public class Assembly
    {
        public string Name { get; set; }
        private readonly AssemblyBuilder _builder;

        public Assembly(string name, SymbolTable table)
        {
            Name = name;
            _builder = new AssemblyBuilder(table);
        }

        public Assembly Generate(List<INode> trees, bool intermediate, List<string> assemblyRefs, string targetFramework, string outputType, string outputPath = "")
        {
            bool isExe = outputType.Equals("exe", StringComparison.OrdinalIgnoreCase);

            // Create the Mono.Cecil assembly definition
            var assemblyName = new AssemblyNameDefinition(Name, new Version(1, 0, 0, 0));
            var moduleParams = new ModuleParameters
            {
                Kind = isExe ? ModuleKind.Console : ModuleKind.Dll,
                Runtime = TargetRuntime.Net_4_0 // Cecil uses this for PE format; actual TFM is set via references
            };

            var assemblyDef = AssemblyDefinition.CreateAssembly(assemblyName, Name, moduleParams);
            var module = assemblyDef.MainModule;

            // Resolve .NET reference assemblies using Basic.Reference.Assemblies (from Roslyn helper package)
            var resolver = (DefaultAssemblyResolver)module.AssemblyResolver;
            var referenceDirs = GetReferenceAssemblyDirs(targetFramework);

            foreach (var dir in referenceDirs)
            {
                resolver.AddSearchDirectory(dir);
            }

            // Add custom assembly references
            foreach (var reference in assemblyRefs)
            {
                try
                {
                    if (File.Exists(reference))
                    {
                        resolver.AddSearchDirectory(Path.GetDirectoryName(reference)!);
                        continue;
                    }

                    var ass = System.Reflection.Assembly.Load(reference);
                    if (ass != null)
                    {
                        resolver.AddSearchDirectory(Path.GetDirectoryName(ass.Location)!);
                    }
                }
                catch
                {
                    // Assembly not found at load time; may still resolve via search dirs
                }
            }

            // Set the target framework attribute on the assembly
            SetTargetFrameworkAttribute(assemblyDef, targetFramework);

            // Initialize the builder with the assembly
            _builder.Init(assemblyDef);

            // Build all AST trees into CIL
            foreach (var tree in trees)
            {
                _builder.Build(tree);
            }

            // Set the entry point for exe output
            if (isExe)
            {
                if (_builder.EntryPoint != null)
                {
                    assemblyDef.EntryPoint = _builder.EntryPoint;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine("Error: output type is 'exe' but no 'fn main()' entry point was found.");
                    Console.ResetColor();
                    return this;
                }
            }

            // Write the assembly to disk (modern .NET: both exe and dll use .dll extension, run via `dotnet`)
            try
            {
                // Honour an explicit output path; otherwise default to "{Name}.dll" in the working directory.
                var outPath = string.IsNullOrWhiteSpace(outputPath) ? $"{Name}.dll" : outputPath;
                var outDir = Path.GetDirectoryName(outPath);
                if (!string.IsNullOrEmpty(outDir))
                {
                    Directory.CreateDirectory(outDir);
                }

                var writerParams = new WriterParameters();
                assemblyDef.Write(outPath, writerParams);
                Console.WriteLine($"Assembly written to {outPath}");

                // For exe output, also emit a runtimeconfig.json next to the assembly so `dotnet <assembly>` works
                if (isExe)
                {
                    WriteRuntimeConfig(targetFramework, outPath);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Failed to write assembly: {ex.Message}");
                Console.ResetColor();
            }

            return this;
        }

        public void Build()
        {
        }

        private static HashSet<string> GetReferenceAssemblyDirs(string targetFramework)
        {
            IEnumerable<PortableExecutableReference> refs = targetFramework switch
            {
                ".NET Framework 4" => ReferenceAssemblies.Net472,
                "netstandard2.0" => ReferenceAssemblies.NetStandard20,
                _ => Basic.Reference.Assemblies.Net100.References.All
            };

            var dirs = new HashSet<string>();
            foreach (var r in refs)
            {
                if (r.FilePath != null)
                {
                    var dir = Path.GetDirectoryName(r.FilePath);
                    if (dir != null) dirs.Add(dir);
                }
            }

            return dirs;
        }

        private void WriteRuntimeConfig(string targetFramework, string assemblyPath)
        {
            var tfmVersion = targetFramework switch
            {
                ".NET Framework 4" => null, // .NET Framework doesn't use runtimeconfig
                "netstandard2.0" => null,
                _ => "10.0"
            };

            if (tfmVersion == null) return;

            var json = $$"""
                {
                  "runtimeOptions": {
                    "tfm": "net{{tfmVersion}}",
                    "framework": {
                      "name": "Microsoft.NETCore.App",
                      "version": "{{tfmVersion}}.0"
                    }
                  }
                }
                """;

            var configPath = Path.ChangeExtension(assemblyPath, ".runtimeconfig.json");
            File.WriteAllText(configPath, json);
            Console.WriteLine($"Runtime config written to {configPath}");
        }

        private static void SetTargetFrameworkAttribute(AssemblyDefinition assembly, string targetFramework)
        {
            var module = assembly.MainModule;
            var tfm = targetFramework switch
            {
                ".NET Framework 4" => ".NETFramework,Version=v4.7.2",
                "netstandard2.0" => ".NETStandard,Version=v2.0",
                _ => ".NETCoreApp,Version=v10.0"
            };

            // Import the TargetFrameworkAttribute constructor
            var attrType = module.ImportReference(typeof(System.Runtime.Versioning.TargetFrameworkAttribute));
            var attrCtor = module.ImportReference(
                typeof(System.Runtime.Versioning.TargetFrameworkAttribute)
                    .GetConstructor(new[] { typeof(string) }));

            var attribute = new CustomAttribute(attrCtor);
            attribute.ConstructorArguments.Add(
                new CustomAttributeArgument(module.ImportReference(typeof(string)), tfm));

            assembly.CustomAttributes.Add(attribute);
        }
    }
}
