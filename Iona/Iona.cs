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
        [Option('a', "assemblies", Required = false, HelpText = "Additional paths to check for assemblies")]
        public IEnumerable<string> AssemblyPaths { get; set; }
        [Option('r', "references", Required = false, HelpText = "Assemblies that shall be referenced.")]

        public IEnumerable<string> AssemblyRefs { get; set; }
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
        // Read the second arg from the command line
        var fileName = options.InputFiles.FirstOrDefault();
        var code = File.ReadAllText(fileName);
        // Normalize the code(converting \r\n to \n)
        code = code.Replace("\r\n", "\n");
        
        Console.WriteLine("Options:");

        if (options.Intermediate)
        {
            Console.WriteLine("- Intermediate");
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Compiling {0}", fileName);
        Console.ForegroundColor = ConsoleColor.White;
        
        compiler.Compile(
            "App", 
            new List<CompilationUnit> { new CompilationUnit { Source = code, Name = fileName }}, 
            options.Intermediate, 
            options.AssemblyPaths?.ToList() ?? new List<string>(),
            options.AssemblyRefs?.ToList() ?? new List<string>()
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