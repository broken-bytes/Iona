namespace Iona.Builtins
{
    public struct Int8
    {
        public static Int8 Random() => new Int8(new Random().Next());

        internal readonly int value;

        public Int8(int value)
        {
            this.value = value;
        }

        public Int8(Float value)
        {
            this.value = (int)value.value;
        }

        public static Int8 operator +(Int8 a, Int8 b) => new Int8(a.value + b.value);
        public static Int8 operator -(Int8 a, Int8 b) => new Int8(a.value - b.value);
        public static Int8 operator *(Int8 a, Int8 b) => new Int8(a.value * b.value);
        public static Int8 operator /(Int8 a, Int8 b) => new Int8(a.value / b.value);

        public static bool operator ==(Int8 a, Int8 b) => a.value == b.value;
        public static bool operator !=(Int8 a, Int8 b) => a.value != b.value;
        public static bool operator <(Int8 a, Int8 b) => a.value < b.value;
        public static bool operator >(Int8 a, Int8 b) => a.value > b.value;
        public static bool operator <=(Int8 a, Int8 b) => a.value <= b.value;
        public static bool operator >=(Int8 a, Int8 b) => a.value >= b.value;

        public static Int8 operator ++(Int8 a) => new Int8(a.value + 1);
        public static Int8 operator --(Int8 a) => new Int8(a.value - 1);

        public override bool Equals(object? obj)
        {
            if (obj is Int8 @int)
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