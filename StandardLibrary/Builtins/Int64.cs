namespace Iona.Builtins
{
    public struct Int64
    {
        public static Int64 Random() => new Int64(new Random().Next());

        internal readonly int value;

        public Int64(int value)
        {
            this.value = value;
        }

        public Int64(Float value)
        {
            this.value = (int)value.value;
        }

        public static Int64 operator +(Int64 a, Int64 b) => new Int64(a.value + b.value);
        public static Int64 operator -(Int64 a, Int64 b) => new Int64(a.value - b.value);
        public static Int64 operator *(Int64 a, Int64 b) => new Int64(a.value * b.value);
        public static Int64 operator /(Int64 a, Int64 b) => new Int64(a.value / b.value);

        public static bool operator ==(Int64 a, Int64 b) => a.value == b.value;
        public static bool operator !=(Int64 a, Int64 b) => a.value != b.value;
        public static bool operator <(Int64 a, Int64 b) => a.value < b.value;
        public static bool operator >(Int64 a, Int64 b) => a.value > b.value;
        public static bool operator <=(Int64 a, Int64 b) => a.value <= b.value;
        public static bool operator >=(Int64 a, Int64 b) => a.value >= b.value;

        public static Int64 operator ++(Int64 a) => new Int64(a.value + 1);
        public static Int64 operator --(Int64 a) => new Int64(a.value - 1);

        public override bool Equals(object? obj)
        {
            if (obj is Int64 @int)
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