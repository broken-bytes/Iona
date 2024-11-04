using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Generator.Types
{
    internal class MethodDefinition
    {
        public string Name { get; }
        public MethodAttributes Attributes { get; set; }
        public TypeReference ReturnType { get; }
        public bool HasThis { get; set; }
        private ILEmitter? _currentEmitter;
        public FunctionBody Body;
        public bool IsStatic { get; set; }
        public List<ParameterDefinition> Parameters { get; set; } = new();

        internal MethodDefinition(string name, MethodAttributes attributes, TypeReference returnType)
        {
            Name = name;
            Attributes = attributes;
            ReturnType = returnType;
            _currentEmitter = new ILEmitter();
            Body = new FunctionBody();
        }

        public void Write(string filePath)
        {

        }
    }
}
