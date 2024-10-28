namespace Shared
{
    public struct Metadata
    {
        public string File { get; set; }
        public int LineStart { get; set; }
        public int LineEnd { get; set; }
        public int ColumnStart { get; set; }
        public int ColumnEnd { get; set; }

        public override string ToString()
        {
            return $"File: {File}(Line: {LineStart}-{LineEnd}, Column: {ColumnStart}-{ColumnEnd})";
        }
    }
}
