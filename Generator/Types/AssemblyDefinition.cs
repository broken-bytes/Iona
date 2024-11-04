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
        private ILEmitter? _currentEmitter;
        public ILEmitter Emitter => _currentEmitter!;
        public List<ModuleDefinition> Modules { get; set; } = new();

        internal AssemblyDefinition(string name)
        {
            Name = name;
            _currentEmitter = new ILEmitter();
        }
    }
}
