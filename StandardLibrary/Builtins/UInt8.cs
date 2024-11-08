namespace Iona.Builtins
{
    public struct UInt8
    {
        public static UInt8 Random() => new UInt8(new Random().Next());

        internal readonly int value;

        public UInt8(int value)
        {
            this.value = value;
        }

        public UInt8(Float value)
        {
            this.value = (int)value.value;
        }

        public static UInt8 operator +(UInt8 a, UInt8 b) => new UInt8(a.value + b.value);
        public static UInt8 operator -(UInt8 a, UInt8 b) => new UInt8(a.value - b.value);
        public static UInt8 operator *(UInt8 a, UInt8 b) => new UInt8(a.value * b.value);
        public static UInt8 operator /(UInt8 a, UInt8 b) => new UInt8(a.value / b.value);

        public static bool operator ==(UInt8 a, UInt8 b) => a.value == b.value;
        public static bool operator !=(UInt8 a, UInt8 b) => a.value != b.value;
        public static bool operator <(UInt8 a, UInt8 b) => a.value < b.value;
        public static bool operator >(UInt8 a, UInt8 b) => a.value > b.value;
        public static bool operator <=(UInt8 a, UInt8 b) => a.value <= b.value;
        public static bool operator >=(UInt8 a, UInt8 b) => a.value >= b.value;

        public static UInt8 operator ++(UInt8 a) => new UInt8(a.value + 1);
        public static UInt8 operator --(UInt8 a) => new UInt8(a.value - 1);

        public override bool Equals(object? obj)
        {
            if (obj is UInt8 @int)
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