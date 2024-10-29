namespace Shared
{
    public interface IErrorCollector
    {
        public List<CompilerError> Errors { get; }
        public void Collect(CompilerError error);
    }
}
