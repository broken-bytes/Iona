namespace Shared
{
    public class CompilerWarning
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public Metadata Meta { get; set; }

        public CompilerWarning(CompilerWarningCode code, string message, Metadata meta)
        {

            Code = GetCodeString(code);
            Message = message;
            Meta = meta;
        }

        public override string ToString()
        {
            return $"Error {Code}: {Message} at {Meta}";
        }

        private static string GetCodeString(CompilerWarningCode code)
        {
            switch (code)
            {
                case CompilerWarningCode.SymbolShadowing:
                    return "W0000";
                default:
                    return "UnknownWarning";
            }
        }
    }
}
