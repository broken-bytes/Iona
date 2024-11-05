namespace Iona.Builtins
{
    public class Container
    {
        public bool IsEmpty => _isEmpty;
        private bool _isEmpty;

        public Int Count => new Int(0);
    }
}
