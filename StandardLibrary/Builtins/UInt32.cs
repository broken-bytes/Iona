namespace Iona.Builtins
{
    public struct UInt32
    {
        public static UInt32 Random() => new UInt32(new Random().Next());

        internal readonly int value;

        public UInt32(int value)
        {
            this.value = value;
        }

        public UInt32(Float value)
        {
            this.value = (int)value.value;
        }

        public static UInt32 operator +(UInt32 a, UInt32 b) => new UInt32(a.value + b.value);
        public static UInt32 operator -(UInt32 a, UInt32 b) => new UInt32(a.value - b.value);
        public static UInt32 operator *(UInt32 a, UInt32 b) => new UInt32(a.value * b.value);
        public static UInt32 operator /(UInt32 a, UInt32 b) => new UInt32(a.value / b.value);

        public static bool operator ==(UInt32 a, UInt32 b) => a.value == b.value;
        public static bool operator !=(UInt32 a, UInt32 b) => a.value != b.value;
        public static bool operator <(UInt32 a, UInt32 b) => a.value < b.value;
        public static bool operator >(UInt32 a, UInt32 b) => a.value > b.value;
        public static bool operator <=(UInt32 a, UInt32 b) => a.value <= b.value;
        public static bool operator >=(UInt32 a, UInt32 b) => a.value >= b.value;

        public static UInt32 operator ++(UInt32 a) => new UInt32(a.value + 1);
        public static UInt32 operator --(UInt32 a) => new UInt32(a.value - 1);

        public override bool Equals(object? obj)
        {
            if (obj is UInt32 @int)
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