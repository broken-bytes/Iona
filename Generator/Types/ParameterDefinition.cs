using System.Reflection;

namespace Generator.Types
{
    internal class ParameterDefinition
    {
        public string Name { get; }
        public ParameterAttributes Attributes { get; }
        public TypeReference TypeReference { get; }
        public int Index { get; set; }
        internal ParameterDefinition(string name, ParameterAttributes attributes, TypeReference typeReference)
        {
            Name = name;
            Attributes = attributes;
            TypeReference = typeReference;
        }
    }
}
