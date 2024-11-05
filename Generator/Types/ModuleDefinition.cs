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
        private ILProcessor? _processor;

        public ILProcessor Processor => _processor!;

        public List<TypeDefinition> Types { get; set; } = new List<TypeDefinition>();
        public List<MethodDefinition> Methods { get; set; } = new List<MethodDefinition>();

        internal ModuleDefinition(string name)
        {
            Name = name;
            _processor = new ILProcessor();
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
