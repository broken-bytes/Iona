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
    }
    
    static void Main(String[] args)
    {
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

        compiler.Compile("App", new List<CompilationUnit> { new CompilationUnit { Source = code, Name = fileName }}, options.Intermediate); 
    }

    static void OnError(IEnumerable<Error> errors)
    {
        foreach (var error in errors)
        {
            Console.WriteLine(error);
        }
    }
}