//|--- Bool.cs -------------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace Iona.Builtins
{
    public struct Bool
    {
        private readonly bool value;

        public Bool(bool value)
        {
            this.value = value;
        }

        public static Bool operator !(Bool a) => new Bool(!a.value);
        public static Bool operator &(Bool a, Bool b) => new Bool(a.value && b.value);
        public static Bool operator |(Bool a, Bool b) => new Bool(a.value || b.value);
        public static Bool operator ^(Bool a, Bool b) => new Bool(a.value ^ b.value);

        public static bool operator ==(Bool a, Bool b) => a.value == b.value;
        public static bool operator !=(Bool a, Bool b) => a.value != b.value;

        public override bool Equals(object? obj)
        {
            if (obj is Bool @bool)
            {
                return this == @bool;
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
    }
}
