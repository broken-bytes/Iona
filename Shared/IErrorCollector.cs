namespace Shared
{
    public interface IErrorCollector
    {
        public void Collect(CompilerError error);
    }
}
