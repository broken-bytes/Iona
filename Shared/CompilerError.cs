namespace Shared
{
    public class CompilerError
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public Metadata Meta { get; set; }
        
        public List<(int, string)> Context { get; set; }

        public CompilerError(CompilerErrorCode code, string message, Metadata meta) {
            
            Code = GetCodeString(code);
            Message = message;
            Meta = meta;
            
            Context = new ();
            
            // Read LineEnd - LineStart + 2 Lines Starting At LineStart - 1
            var lineStart = meta.LineStart - 1;
            var lineEnd = meta.LineEnd + 1;
            
            var lines = File.ReadLines(meta.File);

            var current = 0;

            foreach (var line in lines)
            {
                current++;

                if (current >= lineStart && current <= lineEnd)
                {
                    Context.Add((current, line));
                }

                if (current > lineEnd)
                {
                    break;
                }
            }
        }

        public override string ToString()
        {
            return $"Error[{Code}]: {Message}\n--> {Meta}\n  See https://ionalang.org/reference/errors#{Code}";
        }

        public void Log()
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write($"{Meta.File}");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(":");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(Meta.LineStart);
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(":");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{Meta.ColumnStart}");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" - ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write($"error ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"{Code}: ");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Message);
            Console.ResetColor();

            foreach (var line in Context)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                var lineNumberWidth = line.Item1.ToString().Count() + 1;
                if (Meta.LineStart >= line.Item1 && Meta.LineEnd <= line.Item1)
                {
                    Console.Write($"{line.Item1} | ");
                }
                else
                {
                    Console.Write($"{new string(' ', lineNumberWidth)}| ");
                }
                Console.ResetColor();

                for (int x = 0; x < line.Item2.Length; x++)
                {
                    if (x >= Meta.ColumnStart - 1 && x < Meta.ColumnEnd && Meta.LineStart >= line.Item1 && Meta.LineEnd <= line.Item1)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    
                    Console.Write(line.Item2[x]);
                    Console.ResetColor();
                }
                
                // When the current line is the error source, also underline the characters
                if (Meta.LineStart >= line.Item1 && Meta.LineEnd <= line.Item1)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"{new string(' ', lineNumberWidth)}| ");
                    Console.WriteLine(new string(' ', Meta.ColumnStart - 1) + new string('~', Meta.ColumnEnd - Meta.ColumnStart));
                }
                else
                {
                    Console.WriteLine();
                }
            }
            
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"See https://ionalang.org/docs/reference/errors#{Code}");
            Console.WriteLine();
        }

        private static string GetCodeString(CompilerErrorCode code)
        {
            switch (code)
            {
                case CompilerErrorCode.SyntaxError:
                    return "C0001";
                case CompilerErrorCode.UndefinedName:
                    return "C0002";
                case CompilerErrorCode.TypeMismatch:
                    return "C0003";
                case CompilerErrorCode.UndefinedTopLevel:
                    return "C0004";
                case CompilerErrorCode.TypeDoesNotContainProperty:
                    return "C0006";
                case CompilerErrorCode.AmbiguousTypes:
                    return "C0007";
                case CompilerErrorCode.ExpectedMember:
                    return "C0008";
                case CompilerErrorCode.MissingTypeAnnotation:
                    return "C0009";
                case CompilerErrorCode.NoBinaryOverload:
                    return "C0010";
                case CompilerErrorCode.AmbigiousOperatorOverload:
                    return "C0011";
                default:
                    return "UnknownError";
            }
        }
    }
}
