using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Generator.Types
{
    internal class TypeDefinition
    {
        public TypeAttributes Attributes { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
        public TypeReference BaseType { get; set; }
        public List<TypeReference> Interfaces { get; set; }
        public List<FieldDefinition> Fields { get; set; } = new List<FieldDefinition>();
        public List<MethodDefinition> Methods { get; set; } = new List<MethodDefinition>();
        public List<PropertyDefinition> Properties { get; set; } = new List<PropertyDefinition>();

        public TypeDefinition(string nspace, string name, TypeAttributes typeAttributes, TypeReference baseType)
        {
            Namespace = nspace;
            Name = name;
            Attributes = typeAttributes;
            BaseType = baseType;
        }

        public void Write(string filePath)
        {
            foreach (var field in Fields)
            {
                field.Write(filePath);
            }

            foreach (var property in Properties)
            {
                property.Write(filePath);
            }

            foreach (var method in Methods)
            {
                method.Write(filePath);
            }
        }
    }
}
