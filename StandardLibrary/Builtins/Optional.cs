namespace Iona.Builtins
{
    public struct Optional<T>
    {
        public static Optional<T> None => new(default);

        public bool HasValue { get; private set; }
        public T? Value { get; }

        public Optional(T value)
        {
            Value = value;
            HasValue = true;
        }

        public Optional()
        {
            Value = default;
            HasValue = false;
        }

        public static bool operator==(Optional<T> left, Optional<T> right)
        {
            return left.HasValue switch
            {
                true when right.HasValue => left.Value!.Equals(right.Value),
                false when !right.HasValue => true,
                _ => false
            };
        }
        
        public static bool operator!=(Optional<T> left, Optional<T> right)
        {
            return true;
        }
    }
}