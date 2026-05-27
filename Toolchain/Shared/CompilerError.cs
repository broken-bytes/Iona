//|--- CompilerError.cs ----------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

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

            if (string.IsNullOrEmpty(meta.File) || !File.Exists(meta.File))
            {
                return;
            }

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
            // Compute gutter width: right-align line numbers to the widest one
            var gutterWidth = Context.Count > 0
                ? Context.Max(l => l.Item1).ToString().Length
                : 1;

            // Header: --> file:line:column
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write($"{new string(' ', gutterWidth)}");
            Console.Write(" --> ");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{Meta.File}");
            Console.Write(":");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(Meta.LineStart);
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(":");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(Meta.ColumnStart);
            Console.ResetColor();

            // Error label: error[C0018]: message
            Console.ForegroundColor = ConsoleColor.Red;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write($"{new string(' ', gutterWidth)} ");
            Console.Write($"  error");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{Code}]");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(": ");
            Console.WriteLine(Message);
            Console.ResetColor();

            // Empty gutter separator
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"{new string(' ', gutterWidth + 1)}|");
            Console.ResetColor();

            foreach (var line in Context)
            {
                var lineNum = line.Item1;
                var lineText = line.Item2;
                var isErrorLine = Meta.LineStart <= lineNum && Meta.LineEnd >= lineNum;

                // Line number, right-aligned
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write($"{lineNum.ToString().PadLeft(gutterWidth)} | ");
                Console.ResetColor();

                // Source text, highlighting error span on the error line
                for (int x = 0; x < lineText.Length; x++)
                {
                    if (isErrorLine && x >= Meta.ColumnStart - 1 && x < Meta.ColumnEnd - 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                    }

                    Console.Write(lineText[x]);
                    Console.ResetColor();
                }

                Console.WriteLine();

                // Underline on the error line
                if (isErrorLine)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"{new string(' ', gutterWidth + 1)}| ");
                    var underlineLen = Math.Max(1, Meta.ColumnEnd - Meta.ColumnStart);
                    Console.WriteLine(new string(' ', Meta.ColumnStart - 1) + new string('~', underlineLen));
                    Console.ResetColor();
                }
            }

            // Closing empty gutter
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"{new string(' ', gutterWidth + 1)}|");
            Console.ResetColor();

            // Docs link
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"{new string(' ', gutterWidth + 1)}= ");
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
                case CompilerErrorCode.AmbiguousOperatorOverload:
                    return "C0011";
                case CompilerErrorCode.AmbiguousFunctionCall:
                    return "C0012";
                case CompilerErrorCode.MissingParameterName:
                    return "C0013";
                case CompilerErrorCode.MissingColonAfterParameterName:
                    return "C0014";
                case CompilerErrorCode.AmbiguousTypeReference:
                    return "C0015";
                case CompilerErrorCode.CannotInferType:
                    return "C0016";
                case CompilerErrorCode.VariableNotAllowedInTopLevel:
                    return "C0017";
                case CompilerErrorCode.ImmutableVariableAssignment:
                    return "C0018";
                case CompilerErrorCode.NoMatchingConstructorForArgs:
                    return "C0019";
                case CompilerErrorCode.TypeDoesNotContainMethod:
                    return "C0020";
                case CompilerErrorCode.InaccessibleMember:
                    return "C0021";
                case CompilerErrorCode.MutatingInNonMutatingFunc:
                    return "C0022";
                case CompilerErrorCode.MutablePropertyInRecord:
                    return "C0023";
                case CompilerErrorCode.MutatingFuncInRecord:
                    return "C0024";
                case CompilerErrorCode.OptionalNotUnwrapped:
                    return "C0025";
                case CompilerErrorCode.ReturnTypeMismatch:
                    return "C0026";
                case CompilerErrorCode.ContractConformanceMissingMember:
                    return "C0027";
                case CompilerErrorCode.ValueTypeCannotInheritClass:
                    return "C0028";
                case CompilerErrorCode.ForceUnwrapOfNonOptional:
                    return "C0029";
                case CompilerErrorCode.OptionalArgumentNotUnwrapped:
                    return "C0030";
                default:
                    return "UnknownError";
            }
        }
    }
}
