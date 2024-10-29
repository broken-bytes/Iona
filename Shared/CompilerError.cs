namespace Shared
{
    public class CompilerError
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public Metadata Meta { get; set; }

        public CompilerError(CompilerErrorCode code, string message, Metadata meta) {
            
            Code = GetCodeString(code);
            Message = message;
            Meta = meta;
        }

        public override string ToString()
        {
            return $"Error {Code}: {Message} at {Meta}";
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
                default:
                    return "UnknownError";
            }
        }
    }
}
