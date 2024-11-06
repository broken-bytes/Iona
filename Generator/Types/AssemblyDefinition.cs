using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generator.Types
{
    internal class AssemblyDefinition
    {
        public string Name { get; }
        public List<ModuleDefinition> Modules { get; set; } = new();

        internal AssemblyDefinition(string name)
        {
            Name = name;
        }
    }
}
