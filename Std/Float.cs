namespace Builtins
{
    public struct Float
    {
        public static Float Random() => new Float(new Random().Next());

        internal readonly float value;

        public Float(float value)
        {
            this.value = value;
        }

        public Float(Int value)
        {
            this.value = value.value;
        }

        public static Float operator +(Float a, Float b) => new Float(a.value + b.value);
        public static Float operator -(Float a, Float b) => new Float(a.value - b.value);
        public static Float operator *(Float a, Float b) => new Float(a.value * b.value);
        public static Float operator /(Float a, Float b) => new Float(a.value / b.value);

        public static bool operator ==(Float a, Float b) => a.value == b.value;
        public static bool operator !=(Float a, Float b) => a.value != b.value;
        public static bool operator <(Float a, Float b) => a.value < b.value;
        public static bool operator >(Float a, Float b) => a.value > b.value;
        public static bool operator <=(Float a, Float b) => a.value <= b.value;
        public static bool operator >=(Float a, Float b) => a.value >= b.value;

        public static Float operator ++(Float a) => new Float(a.value + 1);
        public static Float operator --(Float a) => new Float(a.value - 1);

        public override bool Equals(object? obj)
        {
            if (obj is Float @Float)
            {
                return this == @Float;
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

        public float Rounded()
        {
            return (int)value;
        }
    }
}
