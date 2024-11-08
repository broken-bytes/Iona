namespace Iona.Builtins
{
    public struct UInt16
    {
        public static UInt16 Random() => new UInt16(new Random().Next());

        internal readonly int value;

        public UInt16(int value)
        {
            this.value = value;
        }

        public UInt16(Float value)
        {
            this.value = (int)value.value;
        }

        public static UInt16 operator +(UInt16 a, UInt16 b) => new UInt16(a.value + b.value);
        public static UInt16 operator -(UInt16 a, UInt16 b) => new UInt16(a.value - b.value);
        public static UInt16 operator *(UInt16 a, UInt16 b) => new UInt16(a.value * b.value);
        public static UInt16 operator /(UInt16 a, UInt16 b) => new UInt16(a.value / b.value);

        public static bool operator ==(UInt16 a, UInt16 b) => a.value == b.value;
        public static bool operator !=(UInt16 a, UInt16 b) => a.value != b.value;
        public static bool operator <(UInt16 a, UInt16 b) => a.value < b.value;
        public static bool operator >(UInt16 a, UInt16 b) => a.value > b.value;
        public static bool operator <=(UInt16 a, UInt16 b) => a.value <= b.value;
        public static bool operator >=(UInt16 a, UInt16 b) => a.value >= b.value;

        public static UInt16 operator ++(UInt16 a) => new UInt16(a.value + 1);
        public static UInt16 operator --(UInt16 a) => new UInt16(a.value - 1);

        public override bool Equals(object? obj)
        {
            if (obj is UInt16 @int)
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