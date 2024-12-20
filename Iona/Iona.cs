﻿using Compiler;
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
        
        [Option('a', "assemblies", Required = false, HelpText = "Additional paths to check for assemblies")]
        public IEnumerable<string> AssemblyPaths { get; set; }
        
        [Option('r', "references", Required = false, HelpText = "Assemblies that shall be referenced.")]
        public IEnumerable<string> AssemblyRefs { get; set; }

        [Option('f', "framework", Required = false,
            HelpText =
                "The .NET framework to use. Allowed values are '.NET Framework', .NET 8, and >NET Standard 2. Default is '.NET 8'")]
        public string TargetFramework { get; set; } = "";
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
        
        compiler.Compile(
            "App", 
            compilationUnits, 
            options.Intermediate, 
            options.Debug,
            options.AssemblyPaths?.ToList() ?? new List<string>(),
            options.AssemblyRefs?.ToList() ?? new List<string>(),
            options.TargetFramework
        ); 
    }

    static void OnError(IEnumerable<Error> errors)
    {
        foreach (var error in errors)
        {
            Console.WriteLine(error);
        }
    }
}