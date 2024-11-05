using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Generator.Types
{
    internal class FieldDefinition
    {
        public string Name { get; }
        public FieldAttributes Attributes { get; }
        public TypeReference TypeReference { get; }
        public int Index { get; set; }
        internal FieldDefinition(string name, FieldAttributes attributes, TypeReference typeReference)
        {
            Name = name;
            Attributes = attributes;
            TypeReference = typeReference;
        }

        public void Write(StreamWriter stream)
        {
            var type = TypeReference.IsReferenceType ? "class" : "valuetype";
            stream.WriteLine($".field private initonly {type} [{TypeReference.Assembly}]{TypeReference.FullName} '{Name}'");
        }
    }
}
