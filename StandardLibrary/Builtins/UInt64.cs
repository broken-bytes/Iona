namespace Iona.Builtins
{
    public struct UInt64
    {
        public static UInt64 Random() => new UInt64(new Random().Next());

        internal readonly int value;

        public UInt64(int value)
        {
            this.value = value;
        }

        public UInt64(Float value)
        {
            this.value = (int)value.value;
        }

        public static UInt64 operator +(UInt64 a, UInt64 b) => new UInt64(a.value + b.value);
        public static UInt64 operator -(UInt64 a, UInt64 b) => new UInt64(a.value - b.value);
        public static UInt64 operator *(UInt64 a, UInt64 b) => new UInt64(a.value * b.value);
        public static UInt64 operator /(UInt64 a, UInt64 b) => new UInt64(a.value / b.value);

        public static bool operator ==(UInt64 a, UInt64 b) => a.value == b.value;
        public static bool operator !=(UInt64 a, UInt64 b) => a.value != b.value;
        public static bool operator <(UInt64 a, UInt64 b) => a.value < b.value;
        public static bool operator >(UInt64 a, UInt64 b) => a.value > b.value;
        public static bool operator <=(UInt64 a, UInt64 b) => a.value <= b.value;
        public static bool operator >=(UInt64 a, UInt64 b) => a.value >= b.value;

        public static UInt64 operator ++(UInt64 a) => new UInt64(a.value + 1);
        public static UInt64 operator --(UInt64 a) => new UInt64(a.value - 1);

        public override bool Equals(object? obj)
        {
            if (obj is UInt64 @int)
            {
                return this == @int;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public bool IsEven()
        {
            return value % 2 == 0;
        }
    }
}