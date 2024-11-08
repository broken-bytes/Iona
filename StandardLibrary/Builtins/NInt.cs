namespace Iona.Builtins
{
    public struct NInt
    {
        public static NInt Random() => new NInt(new Random().Next());

        internal readonly nint value;

        public NInt(nint value)
        {
            this.value = value;
        }

        public NInt(Float value)
        {
            this.value = (nint)value.value;
        }
        
        public static NInt operator +(NInt a, NInt b) => new NInt(a.value + b.value);
        public static NInt operator -(NInt a, NInt b) => new NInt(a.value - b.value);
        public static NInt operator *(NInt a, NInt b) => new NInt(a.value * b.value);
        public static NInt operator /(NInt a, NInt b) => new NInt(a.value / b.value);

        public static bool operator ==(NInt a, NInt b) => a.value == b.value;
        public static bool operator !=(NInt a, NInt b) => a.value != b.value;
        public static bool operator <(NInt a, NInt b) => a.value < b.value;
        public static bool operator >(NInt a, NInt b) => a.value > b.value;
        public static bool operator <=(NInt a, NInt b) => a.value <= b.value;
        public static bool operator >=(NInt a, NInt b) => a.value >= b.value;

        public static NInt operator ++(NInt a) => new NInt(a.value + 1);
        public static NInt operator --(NInt a) => new NInt(a.value - 1);

        public override bool Equals(object? obj)
        {
            if (obj is NInt @int)
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