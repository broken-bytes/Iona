namespace Iona.Builtins
{
    public struct Int16
    {
        public static Int16 Random() => new Int16(new Random().Next());

        internal readonly int value;

        public Int16(int value)
        {
            this.value = value;
        }

        public Int16(Float value)
        {
            this.value = (int)value.value;
        }

        public static Int16 operator +(Int16 a, Int16 b) => new Int16(a.value + b.value);
        public static Int16 operator -(Int16 a, Int16 b) => new Int16(a.value - b.value);
        public static Int16 operator *(Int16 a, Int16 b) => new Int16(a.value * b.value);
        public static Int16 operator /(Int16 a, Int16 b) => new Int16(a.value / b.value);

        public static bool operator ==(Int16 a, Int16 b) => a.value == b.value;
        public static bool operator !=(Int16 a, Int16 b) => a.value != b.value;
        public static bool operator <(Int16 a, Int16 b) => a.value < b.value;
        public static bool operator >(Int16 a, Int16 b) => a.value > b.value;
        public static bool operator <=(Int16 a, Int16 b) => a.value <= b.value;
        public static bool operator >=(Int16 a, Int16 b) => a.value >= b.value;

        public static Int16 operator ++(Int16 a) => new Int16(a.value + 1);
        public static Int16 operator --(Int16 a) => new Int16(a.value - 1);

        public override bool Equals(object? obj)
        {
            if (obj is Int16 @int)
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