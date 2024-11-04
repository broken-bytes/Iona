using Symbols.Symbols;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generator
{
    internal class ILProcessor
    {
        private StringBuilder _stream;

        internal ILProcessor()
        {
            _stream = new StringBuilder();
            // Every stream starts with a `{` and ends with a `}`
            _stream.AppendLine("{");
        }

        internal void Emit(OpCode opCode, object operand)
        {
            var op = Utils.GetOpCode(opCode);
            _stream.AppendLine($"{opCode}_{operand}");
        }

        internal void EmitNewIstance(TypeSymbol type, InitSymbol ctor, string assembly) {
            var ctorParams = ctor.Symbols.OfType<ParameterSymbol>().Select(p => p.Type.FullyQualifiedName).ToArray();
            _stream.AppendLine($"newobj instance [{assembly}]{type.FullyQualifiedName}::ctor");
        }

        internal void Emit(OpCode opCode)
        {
            _stream.AppendLine($"{opCode}");
        }

        internal void Write(string filePath)
        {
            using (StreamWriter writer = File.AppendText(filePath))
            {
                writer.WriteLine(_stream.ToString());
            }
        }
    }
}
