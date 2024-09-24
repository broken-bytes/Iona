namespace ASTVisualizer
{
    public static class ASTVisualizerFactory
    {
        public static IASTVisualizer Create()
        {
            return new ASTVisualizer();
        }
    }
}
