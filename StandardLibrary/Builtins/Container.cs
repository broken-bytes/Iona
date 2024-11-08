namespace Iona.Builtins
{
    public class Container
    {
        public bool IsEmpty => _isEmpty;
        private bool _isEmpty;

        public NInt Count => new NInt(0);
    }
}
