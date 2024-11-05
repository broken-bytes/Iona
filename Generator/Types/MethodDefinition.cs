using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
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

        public void Write(StreamWriter stream)
        {
            var headerBuilder = new StringBuilder();
            headerBuilder.Append(".method ");
            if (Attributes.HasFlag(MethodAttributes.Public))
            {
                headerBuilder.Append("public ");
            }
            else if (Attributes.HasFlag(MethodAttributes.Private))
            {
                headerBuilder.Append("private ");
            }
            else
            {
                headerBuilder.Append("assembly ");
            }
            if (IsStatic)
            {
                headerBuilder.Append("static ");
            }
            headerBuilder.Append("hidebysig ");
            if (Attributes.HasFlag(MethodAttributes.SpecialName))
            {
                headerBuilder.Append("specialname ");
            }

            if (Attributes.HasFlag(MethodAttributes.RTSpecialName))
            {
                headerBuilder.Append("rtspecialname ");
            }

            headerBuilder.Append("\n\t\t");

            if (HasThis)
            {
                headerBuilder.Append("instance ");
            }

            var ret = ReturnType.IsReferenceType ? "class" : "valuetype";

            headerBuilder.Append($"{ret} [{ReturnType.Assembly}]{ReturnType.FullName}");
            headerBuilder.Append($" {Name}");
            headerBuilder.Append(" (");
            if (Parameters.Count > 0)
            {
                foreach (var parameter in Parameters)
                {
                    var type = parameter.TypeReference.IsReferenceType ? "class" : "valuetype";
                    headerBuilder.Append($"{type} [{parameter.TypeReference.Assembly}]{parameter.TypeReference.FullName} '{parameter.Name}', ");
                }
                headerBuilder.Remove(headerBuilder.Length - 2, 2);
            }
            headerBuilder.Append(") ");
            headerBuilder.Append("cil managed\n");
            stream.Write(headerBuilder.ToString());
            var bodyBuilder = new StringBuilder();
            bodyBuilder.Append("{\n");
            bodyBuilder.Append($"\t\t.maxstack 8\n");
            bodyBuilder.Append("\t\tret\n");
            bodyBuilder.Append("\t}\n");

            stream.Write("\t");
            stream.Write(bodyBuilder.ToString());
        }
    }
}
