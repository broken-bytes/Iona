using Builtins;
using System;

namespace Dummy
{
    public struct Foo
    {
        public Foo()
        {
            Test test = new Test(3);

            test.value = 2;

            Console.WriteLine(test.ToString());
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
