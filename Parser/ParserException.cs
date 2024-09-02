
namespace Parser
{
    public enum ParserExceptionCode
    {
        UnexpectedToken = 01,
        UnexpectedEndOfFile = 02,
    }

    public class ParserException : Exception
    {
        public ParserExceptionCode Code { get; private set; }
        public int Line { get; private set; }
        public int StartColumn { get; private set; }
        public int EndColumn { get; private set; }
        public string File { get; private set; }

        public ParserException(ParserExceptionCode code, int line, int startColumn, int endColumn, string file) : base(
            $"SYN-{(int)code}:{ParserExceptionCodeToMessage(code)} at line {line} column {startColumn} in file {file}"
        )
        {
            Code = code;
            Line = line;
            StartColumn = startColumn;
            EndColumn = endColumn;
            File = file;
        }

        private static string ParserExceptionCodeToMessage(ParserExceptionCode code)
        {
            switch (code)
            {
                case ParserExceptionCode.UnexpectedToken:
                    return "Unexpected token";
                case ParserExceptionCode.UnexpectedEndOfFile:
                    return "Unexpected end of file";
            }

            return "Unknown error";
        }
    }
}
