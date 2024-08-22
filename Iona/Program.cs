using Lexer;

class Program
{
    static void Main(String[] args)
    {
        var lexer = new Lexer.Lexer();
        // Read the second arg from the command line
        var fileName = args[0];
        var code = File.ReadAllText(fileName);
        // Normalize the code(converting \r\n to \n)
        code = code.Replace("\r\n", "\n");
        var tokens = lexer.Tokenize(code, fileName);

        foreach (var token in tokens)
        {
            Console.WriteLine(token.Type);
        }
    }
}