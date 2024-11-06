using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generator.Types
{
    internal class ModuleDefinition
    {
        public string Name { get; }


        public List<TypeDefinition> Types { get; set; } = new List<TypeDefinition>();
        public List<MethodDefinition> Methods { get; set; } = new List<MethodDefinition>();

        internal ModuleDefinition(string name)
        {
            Name = name;
        }

        internal void Write(StreamWriter stream)
        {
            foreach (var type in Types)
            {
                type.Write(stream);
            }

            foreach (var method in Methods)
            {
                method.Write(stream);
            }
        }
    }
}
