//|--- Compiler.cs ---------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using ASTLogger;
using ASTVisualizer;
using Lexer;
using Parser;
using Typeck;
using Symbols;
using Symbols.Symbols;
using AST.Nodes;
using Generator;
using System.Collections.Concurrent;
using System.Reflection;
using Shared;
using Assembly = System.Reflection.Assembly;

namespace Compiler
{
    public class Compiler : ICompiler
    {
        private readonly ILexer lexer;
        private readonly IParser parser;
        private readonly ITypeck typeck;
        private readonly IGenerator generator;
        private readonly IErrorCollector errorCollector;
        private readonly IWarningCollector warningCollector;
        private readonly IFixItCollector fixItCollector;

        internal Compiler(
            ILexer lexer, 
            IParser parser, 
            ITypeck typeck, 
            IGenerator generator, 
            IErrorCollector errorCollector,
            IWarningCollector warningCollector,
            IFixItCollector fixItCollector
        )
        {
            this.lexer = lexer;
            this.parser = parser;
            this.typeck = typeck;
            this.generator = generator;
            this.errorCollector = errorCollector;
            this.warningCollector = warningCollector;
            this.fixItCollector = fixItCollector;
        }

        public bool Compile(
            string assemblyName,
            List<CompilationUnit> files,
            bool intermediate,
            bool debug,
            bool emitIr,
            string emitIrJsonDir,
            string emitDiagnosticsJsonPath,
            List<string> assemblyPaths,
            List<string> assemblyRefs,
            string targetFramework,
            string outputType,
            string outputPath
            )
        {
            // Add core System assembly to the references
            // Builtin types (Int32, String, Bool, etc.) are registered programmatically — no Iona.Builtins.dll needed
            if (!assemblyRefs.Contains("System.Runtime"))
            {
                assemblyRefs.Add("System.Runtime");
            }

            var ionaSdkDir = Environment.GetEnvironmentVariable("IONA_SDK_DIR") ?? "";
            if (!string.IsNullOrWhiteSpace(ionaSdkDir) && Directory.Exists(ionaSdkDir))
            {
                assemblyPaths.Add(ionaSdkDir);

                foreach (var dll in Directory.GetFiles(ionaSdkDir, "*.dll"))
                {
                    var name = Path.GetFileNameWithoutExtension(dll);
                    if (!assemblyRefs.Contains(name))
                        assemblyRefs.Add(name);
                }
            }
            // The compiler is made up of several passes:
            // - Lexing
            // - Parsing
            // - AST construction
            // - Symbol table construction
            // - Scope checking (will be done twice, before and after type checking)
            // - Type checking
            // - Code generation
            // - Assembly building
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                // Get the assembly name that failed to load
                string assemblyName = new AssemblyName(args.Name).Name + ".dll";

                // Construct the full path to the assembly in the specified folder
                // First, check if the file is directly contained in some path
                foreach (var path in assemblyPaths)
                {
                    if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                    {
                        continue;
                    }

                    foreach (var file in Directory.GetFiles(path, "*.dll"))
                    {
                        if (file.EndsWith(assemblyName))
                        {
                            return Assembly.LoadFile(file);
                        }
                    }
                }

                // If the assembly was not found in the custom path, return null (continue searching)
                return null;
            };

            SymbolTable globalTable = new SymbolTable();

            ConcurrentBag<INode> asts = new ConcurrentBag<INode>();
            
            var logger = ASTLoggerFactory.Create();
            var visualizer = ASTVisualizerFactory.Create();

            Parallel.ForEach(files, file =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Compiling {file.Name}");
                Console.ForegroundColor = ConsoleColor.White;
                var tokens = lexer.Tokenize(file.Source, file.Name);
                var ast = parser.Parse(tokens, assemblyName);

                // Auto-inject `use Iona.Builtins` so builtin types resolve without an explicit import
                if (ast is FileNode fileNode)
                {
                    // Auto-inject `use Iona.Builtins` and `use System` so core types resolve without explicit imports
                    foreach (var autoImport in new[] { "Iona.Builtins", "System" })
                    {
                        var hasImport = fileNode.Children
                            .OfType<ImportNode>()
                            .Any(i => i.Name == autoImport);

                        if (!hasImport)
                        {
                            fileNode.Children.Insert(0, new ImportNode(autoImport, fileNode));
                        }
                    }
                }

                asts.Add(ast);
            });

            typeck.AddImportedAssemblySymbols(globalTable, assemblyRefs);
            typeck.DoSemanticAnalysis(asts.OfType<FileNode>().ToList(), assemblyName, globalTable);

            if (debug)
            {
                Parallel.ForEach(asts, ast => logger.Log(ast));
            }

            // Diagnostics-only mode: emit front-end diagnostics as JSON and stop before codegen.
            // Runs whether or not there are errors so the IDE always gets a fresh result.
            if (!string.IsNullOrWhiteSpace(emitDiagnosticsJsonPath))
            {
                var diagnosticsJson = new DiagnosticsJsonSerializer()
                    .Serialize(errorCollector.Errors, warningCollector.Warnings);
                File.WriteAllText(emitDiagnosticsJsonPath, diagnosticsJson);

                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"Wrote diagnostics JSON: {emitDiagnosticsJsonPath} " +
                    $"({errorCollector.Errors.Count} error(s), {warningCollector.Warnings.Count} warning(s))");
                Console.ResetColor();

                return !errorCollector.Errors.Any();
            }

            if (errorCollector.Errors.Any())
            {
                foreach(var error in errorCollector.Errors)
                {
                    error.Log();
                }
                
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Compilation failed. See above errors for details.");
                
                Console.ResetColor();
                
                return false;
            }
            
            if (emitIr || !string.IsNullOrWhiteSpace(emitIrJsonDir))
            {
                var lowering = new IR.IrLowering();
                var printer = new IR.IrPrinter();
                var jsonSerializer = new IR.IrJsonSerializer();

                if (!string.IsNullOrWhiteSpace(emitIrJsonDir))
                {
                    Directory.CreateDirectory(emitIrJsonDir);
                }

                foreach (var ast in asts.OfType<FileNode>())
                {
                    var irModule = lowering.Build(ast);

                    if (emitIr)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(printer.Print(irModule));
                        Console.ResetColor();
                    }

                    if (!string.IsNullOrWhiteSpace(emitIrJsonDir))
                    {
                        var json = jsonSerializer.Serialize(irModule);
                        var fileName = SanitizeModuleFileName(irModule.Name) + ".ir.json";
                        var outPath = Path.Combine(emitIrJsonDir, fileName);
                        File.WriteAllText(outPath, json);
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine($"Wrote IR JSON: {outPath}");
                        Console.ResetColor();
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            GenerateCode(assemblyName, asts.ToList(), globalTable, intermediate, assemblyPaths, assemblyRefs, targetFramework, outputType, outputPath);

            CopyNonFrameworkReferences(assemblyRefs, assemblyPaths, outputPath);

            return true;
        }

        // Framework-dependent deploy: only third-party references travel with the app.
        // System.* / Microsoft.* are served by the shared runtime; copying them next to
        // the exe would shadow the runtime's copy and cause version drift.
        private static void CopyNonFrameworkReferences(
            List<string> assemblyRefs,
            List<string> assemblyPaths,
            string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return;
            }

            var outDir = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Directory.GetCurrentDirectory();
            }

            foreach (var refName in assemblyRefs)
            {
                if (IsFrameworkAssembly(refName))
                {
                    continue;
                }

                var resolved = ResolveReferencePath(refName, assemblyPaths);
                if (resolved == null)
                {
                    continue;
                }

                var dest = Path.Combine(outDir, Path.GetFileName(resolved));
                if (Path.GetFullPath(resolved) == Path.GetFullPath(dest))
                {
                    continue;
                }

                try
                {
                    File.Copy(resolved, dest, overwrite: true);
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine($"Copied {Path.GetFileName(resolved)} -> {outDir}");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Failed to copy {resolved}: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }

        private static bool IsFrameworkAssembly(string name)
        {
            var bare = name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                ? Path.GetFileNameWithoutExtension(name)
                : name;
            return bare.StartsWith("System.", StringComparison.Ordinal)
                || bare.StartsWith("Microsoft.", StringComparison.Ordinal)
                || bare == "System"
                || bare == "mscorlib"
                || bare == "netstandard"
                || bare == "WindowsBase";
        }

        private static string? ResolveReferencePath(string refName, List<string> assemblyPaths)
        {
            if (File.Exists(refName))
            {
                return refName;
            }

            var fileName = refName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                ? refName
                : refName + ".dll";

            foreach (var dir in assemblyPaths)
            {
                if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                {
                    continue;
                }

                var candidate = Path.Combine(dir, fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string SanitizeModuleFileName(string moduleName)
        {
            var baseName = Path.GetFileNameWithoutExtension(moduleName);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "module";
            }

            foreach (var c in Path.GetInvalidFileNameChars())
            {
                baseName = baseName.Replace(c, '_');
            }

            return baseName;
        }

        private void GenerateCode(
            string assemblyName,
            List<INode> asts,
            SymbolTable globalTable,
            bool intermediate,
            List<string> assemblyPaths,
            List<string> assemblyRefs,
            string targetFramework,
            string outputType,
            string outputPath
        )
        {
            var assembly = generator.CreateAssembly(assemblyName, globalTable);
            assembly.Generate(asts, intermediate, assemblyRefs, targetFramework, outputType, outputPath);
        }
    }
}
