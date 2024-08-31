namespace Parser
{
    public static class ParserFactory
    {
        public static IParser Create()
        {

            return new Parser();
        }
    }
}
