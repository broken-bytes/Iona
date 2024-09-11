namespace ASTLogger
{
    public static class ASTLoggerFactory
    {
        public static IASTLogger Create()
        {
            return new ASTLogger();
        }
    }
}
