namespace Iona.Builtins
{
    public struct Int
    {
        public static Int Random() => new Int(new Random().Next());

        internal readonly int value;

        public Int(int value)
        {
            this.value = value;
        }

        public Int(Float value)
        {
            this.value = (int)value.value;
        }

        public static Int operator +(Int a, Int b) => new Int(a.value + b.value);
        public static Int operator -(Int a, Int b) => new Int(a.value - b.value);
        public static Int operator *(Int a, Int b) => new Int(a.value * b.value);
        public static Int operator /(Int a, Int b) => new Int(a.value / b.value);

        public static bool operator ==(Int a, Int b) => a.value == b.value;
        public static bool operator !=(Int a, Int b) => a.value != b.value;
        public static bool operator <(Int a, Int b) => a.value < b.value;
        public static bool operator >(Int a, Int b) => a.value > b.value;
        public static bool operator <=(Int a, Int b) => a.value <= b.value;
        public static bool operator >=(Int a, Int b) => a.value >= b.value;

        public static Int operator ++(Int a) => new Int(a.value + 1);
        public static Int operator --(Int a) => new Int(a.value - 1);

        public override bool Equals(object? obj)
        {
            if (obj is Int @int)
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
