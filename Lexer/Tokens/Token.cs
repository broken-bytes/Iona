namespace Lexer.Tokens
{
    public struct Token
    {
        public TokenFamily Family;
        public TokenType Type;
        public string Value;
        public string File;
        public int Line;
        public int ColumnStart;
        public int ColumnEnd;
        public string Error;
    }
}
