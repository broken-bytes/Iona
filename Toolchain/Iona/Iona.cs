//|--- Iona.cs -------------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using Compiler;
using CommandLine;
class Iona
{
    public class Options
    {
        [Value(0, Required = true, HelpText = "Input files separated by whitespace")]
        public IEnumerable<string> InputFiles { get; set; }
        
        [Option('i', "intermediate", Required = false, HelpText = "Compile to C# only. Do not emit assembly.")]
        public bool Intermediate { get; set; } = false;
        [Option('d', "debug", Required = false, HelpText = "Print debug information (AST)")]
        public bool Debug { get; set; } = false;

        [Option("emit-ir", Required = false, HelpText = "Emit the raw IR to terminal output during compilation.")]
        public bool EmitIr { get; set; } = false;

        [Option("emit-ir-json", Required = false, HelpText = "Emit the IR as JSON files (one per module) into the given directory. Consumed by external backends such as the Kotlin IR backend.")]
        public string EmitIrJsonDir { get; set; } = "";

        [Option("emit-diagnostics-json", Required = false, HelpText = "Run only the front-end (lex/parse/typecheck) and write diagnostics (errors + warnings) as JSON to the given file path. Skips code generation. Consumed by IDE tooling such as the IntelliJ ExternalAnnotator.")]
        public string EmitDiagnosticsJsonPath { get; set; } = "";
        
        [Option('a', "assemblies", Required = false, HelpText = "Additional paths to check for assemblies")]
        public IEnumerable<string> AssemblyPaths { get; set; }
        
        [Option('r', "references", Required = false, HelpText = "Assemblies that shall be referenced.")]
        public IEnumerable<string> AssemblyRefs { get; set; }

        [Option('f', "framework", Required = false,
            HelpText =
                "The .NET framework to use. Allowed values are '.NET Framework', .NET 8, and >NET Standard 2. Default is '.NET 8'")]
        public string TargetFramework { get; set; } = "";

        [Option('t', "target", Required = false,
            HelpText = "Build target: 'dll' (library, default) or 'exe' (console application with main entry point).")]
        public string Target { get; set; } = "dll";

        [Option('o', "output", Required = false,
            HelpText = "Output file path (with extension), e.g. 'bin/MyApp.dll'. The assembly name is taken from the file name. Defaults to 'App.dll' in the working directory.")]
        public string Output { get; set; } = "";
    }
    
    static void Main(String[] args)
    {
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Iona compiler v0.0.1");
        Console.ForegroundColor = ConsoleColor.White;
        CommandLine.Parser.Default.ParseArguments<Options>(args).WithParsed(OnCompile).WithNotParsed(OnError);
    }

    static void OnCompile(Options options)
    {
        var compiler = CompilerFactory.Create();

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        
        if (options.Intermediate || options.AssemblyPaths.Any())
        {
            Console.WriteLine("Options:");
        }
        
        Console.ForegroundColor = ConsoleColor.White;
        if (options.Intermediate)
        {
            Console.WriteLine(" - Intermediate");
        }

        foreach (var path in options.AssemblyPaths)
        {
            Console.WriteLine(" - Search Path " + path);
        }
        
        foreach (var reference in options.AssemblyRefs)
        {
            Console.WriteLine(" - Link " + reference);
        }
        
        Console.WriteLine();

        List<CompilationUnit> compilationUnits = []; 
        
        foreach (var file in options.InputFiles)
        {
            // Read the second arg from the command line
            var code = File.ReadAllText(file);
            // Normalize the code(converting \r\n to \n)
            code = code.Replace("\r\n", "\n");
            
            compilationUnits.Add(new CompilationUnit { Source = code, Name = file });
        }
        
        // The assembly's identity name comes from the output file name (C#/F# convention),
        // falling back to "App" when no explicit output path is given.
        var assemblyName = string.IsNullOrWhiteSpace(options.Output)
            ? "App"
            : Path.GetFileNameWithoutExtension(options.Output);

        var success = compiler.Compile(
            assemblyName,
            compilationUnits,
            options.Intermediate,
            options.Debug,
            options.EmitIr,
            options.EmitIrJsonDir,
            options.EmitDiagnosticsJsonPath,
            options.AssemblyPaths?.ToList() ?? new List<string>(),
            options.AssemblyRefs?.ToList() ?? new List<string>(),
            options.TargetFramework,
            options.Target,
            options.Output
        );

        if (!success)
        {
            Environment.ExitCode = 1;
        }
    }

    static void OnError(IEnumerable<Error> errors)
    {
        foreach (var error in errors)
        {
            Console.WriteLine(error);
        }
    }
}