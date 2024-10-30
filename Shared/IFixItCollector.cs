namespace Shared
{
    public interface IFixItCollector
    {
        public List<FixIt> FixIts { get; }
        public void Collect(FixIt fixit);
    }
}
