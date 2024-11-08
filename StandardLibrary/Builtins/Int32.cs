namespace Iona.Builtins
{
    public struct Int32
    {
        public static Int32 Random() => new Int32(new Random().Next());

        internal readonly int value;

        public Int32(int value)
        {
            this.value = value;
        }

        public Int32(Float value)
        {
            this.value = (int)value.value;
        }

        public static Int32 operator +(Int32 a, Int32 b) => new Int32(a.value + b.value);
        public static Int32 operator -(Int32 a, Int32 b) => new Int32(a.value - b.value);
        public static Int32 operator *(Int32 a, Int32 b) => new Int32(a.value * b.value);
        public static Int32 operator /(Int32 a, Int32 b) => new Int32(a.value / b.value);

        public static bool operator ==(Int32 a, Int32 b) => a.value == b.value;
        public static bool operator !=(Int32 a, Int32 b) => a.value != b.value;
        public static bool operator <(Int32 a, Int32 b) => a.value < b.value;
        public static bool operator >(Int32 a, Int32 b) => a.value > b.value;
        public static bool operator <=(Int32 a, Int32 b) => a.value <= b.value;
        public static bool operator >=(Int32 a, Int32 b) => a.value >= b.value;

        public static Int32 operator ++(Int32 a) => new Int32(a.value + 1);
        public static Int32 operator --(Int32 a) => new Int32(a.value - 1);

        public override bool Equals(object? obj)
        {
            if (obj is Int32 @int)
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