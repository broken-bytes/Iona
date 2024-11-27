namespace Iona.Builtins
{
    public struct Double
    {
        public static Double Random() => new Double(new Random().Next());

        internal readonly double value;

        public Double(double value)
        {
            this.value = value;
        }

        public Double(Float value)
        {
            this.value = ((double)value.value);
        }

        public Double(Int32 value)
        {
            this.value = ((double)value.value);
        }

        public static Double operator +(Double a, Double b) => new Double(a.value + b.value);
        public static Double operator -(Double a, Double b) => new Double(a.value - b.value);
        public static Double operator *(Double a, Double b) => new Double(a.value * b.value);
        public static Double operator /(Double a, Double b) => new Double(a.value / b.value);

        public static bool operator ==(Double a, Double b) => a.value == b.value;
        public static bool operator !=(Double a, Double b) => a.value != b.value;
        public static bool operator <(Double a, Double b) => a.value < b.value;
        public static bool operator >(Double a, Double b) => a.value > b.value;
        public static bool operator <=(Double a, Double b) => a.value <= b.value;
        public static bool operator >=(Double a, Double b) => a.value >= b.value;

        public static Double operator ++(Double a) => new Double(a.value + 1);
        public static Double operator --(Double a) => new Double(a.value - 1);

        public override bool Equals(object? obj)
        {
            if (obj is Double @Double)
            {
                return this == @Double;
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

        public Double Rounded()
        {
            return new Double(Math.Round(value, 2));
        }
    }
}