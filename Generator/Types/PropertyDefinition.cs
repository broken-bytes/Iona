using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Generator.Types
{
    internal class PropertyDefinition
    {
        public string Name { get; }
        public PropertyAttributes Attributes { get; }
        public TypeReference TypeReference { get; }
        internal PropertyDefinition(string name, PropertyAttributes attributes, TypeReference typeReference)
        {
            Name = name;
            Attributes = attributes;
            TypeReference = typeReference;
        }

        public void Write(string filePath)
        {

        }
    }
}
