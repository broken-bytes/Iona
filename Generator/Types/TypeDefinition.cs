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

        public void Write(StreamWriter stream)
        {
            // Write the class header
            stream.Write($".class public beforefieldinit {Namespace}.{Name}\n extends [{BaseType.Assembly}]{BaseType.FullName}\n");
            stream.Write("{\n");

            if (Fields.Count > 0)
            {
                // Write the fields
                stream.Write("\t");
                stream.WriteLine("// Fields");
                foreach (var field in Fields)
                {
                    stream.Write("\t");
                    field.Write(stream);
                }
            }

            if (Properties.Count > 0)
            {
                // Write the properties
                stream.Write("\t");
                stream.WriteLine("// Properties");
                foreach (var property in Properties)
                {
                    stream.Write("\t");
                    property.Write(stream);
                }
            }

            if (Methods.Count > 0)
            {
                // Write the methods
                stream.Write("\t");
                stream.WriteLine("// Methods");
                foreach (var method in Methods)
                {
                    stream.Write("\t");
                    method.Write(stream);
                }
            }

            stream.Write("}\n");
        }
    }
}
