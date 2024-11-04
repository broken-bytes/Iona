using Compiler;

class Iona
{
    static void Main(String[] args)
    {
        var compiler = CompilerFactory.Create();
        // Read the second arg from the command line
        var fileName = args[0];
        var code = File.ReadAllText(fileName);
        // Normalize the code(converting \r\n to \n)
        code = code.Replace("\r\n", "\n");

        compiler.Compile("App", new List<CompilationUnit> { new CompilationUnit { Source = code, Name = fileName }});
    }
}