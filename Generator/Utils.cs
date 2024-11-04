using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generator
{
    internal static class Utils
    {
        internal static string GetOpCode(OpCode opCode)
        {
            switch (opCode)
            {
                case OpCode.Add:
                    return "add";
                case OpCode.Subtract:
                    return "sub";
                case OpCode.Multiply:
                    return "mul";
                case OpCode.Divide:
                    return "div";
                case OpCode.Mod:
                    return "rem";
                case OpCode.Box:
                    return "box";
                case OpCode.Castclass:
                    return "castclass";
                case OpCode.Unbox_Any:
                    return "unbox_Any";
                case OpCode.Call:
                    return "call";
                case OpCode.Newobj:
                    return "newobj";
                case OpCode.LoadArgument:
                    return "ldarg";
                case OpCode.LoadDouble:
                    return "ldc_R8";
                case OpCode.LoadField:
                    return "ldfld";
                case OpCode.LoadString:
                    return "ldstr";
                case OpCode.LoadInt:
                    return "ldc_I4";
                case OpCode.Return:
                    return "ret";
                case OpCode.LoadVariable:
                    return "ldloc";
                case OpCode.SetField:
                    return "stfld";
                case OpCode.SetVariable:
                    return "stloc";
                case OpCode.LoadFloat:
                    return "ldc_R4";
                case OpCode.LoadTrue:
                    return "ldc_I4_1";
                case OpCode.LoadFalse:
                    return "ldc_I4_0";
                case OpCode.Void:
                    return "void";
                default:
                    throw new ArgumentException("Invalid OpCode");
            }
        }

        static string GetOpCode(OpCode opCode, string param)
        {
            return $"{GetOpCode(opCode)}_{param}";
        }
    }
}
