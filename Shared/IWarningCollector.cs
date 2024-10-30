
namespace Shared
{
    public interface IWarningCollector
    {
        public List<CompilerWarning> Warnings { get; }
        public bool HasWarning => Warnings.Any();

        public void Collect(CompilerWarning error);
    }
}