using Builtins;
using System;
using App;

namespace Dummy
{
    public struct Foo
    {
        public Foo()
        {
            var app = new Test();
        }
    }

    public class Program
    {
        public static void Main()
        {
            Foo foo = new();
        }
    }
}
