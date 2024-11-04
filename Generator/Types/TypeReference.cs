using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generator.Types
{
    internal class TypeReference
    {
        public string Name { get; } = string.Empty;
        public string Namespace { get; } = string.Empty;
        public string FullName => $"{Namespace}.{Name}";
        public string Assembly { get; } = string.Empty;

        public TypeReference(string name, string ns, string assembly)
        {
            Name = name;
            Namespace = ns;
            Assembly = assembly;
        }
    }
}
