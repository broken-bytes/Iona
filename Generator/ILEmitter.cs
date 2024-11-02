using AST.Types;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Generator
{
    internal class ILEmitter
    {
        private ILProcessor _processor;

        internal ILEmitter() { }

        internal void SetILProcessor(ILProcessor processor) 
        {
            _processor = processor;
        }

        // ---- Get emitters ----
        internal void Call(MethodReference methodRef)
        {
            _processor.Emit(OpCodes.Call, methodRef);
        }

        internal void CreateObject(MethodDefinition ctor)
        {
            _processor.Emit(OpCodes.Newobj, ctor);
        }

        internal void GetArg(int index)
        {
            _processor.Emit(OpCodes.Ldarg, index);
        }

        internal void GetField(FieldDefinition fieldDef)
        {
            _processor.Emit(OpCodes.Ldfld, fieldDef);
        }

        internal void GetProperty(PropertyDefinition propDef)
        {
            _processor.Emit(OpCodes.Call, propDef.GetMethod);
        }

        internal void GetThis()
        {
            _processor.Emit(OpCodes.Ldarg_0);
        }

        internal void GetVariable(int index)
        {
            _processor.Emit(OpCodes.Ldloc, index);
        }

        // ---- Set emitters ----

        internal void SetField(FieldDefinition fieldDef)
        {
            _processor.Emit(OpCodes.Stfld, fieldDef);
        }

        internal void SetProperty(PropertyDefinition propDef)
        {
            _processor.Emit(OpCodes.Call, propDef.SetMethod);
        }

        internal void SetVariable(int index)
        {
            _processor.Emit(OpCodes.Stloc, index);
        }

        // ---- Operation emitters ----

        internal void BinaryOperation(BinaryOperation operation)
        {
            switch (operation)
            {
                case AST.Types.BinaryOperation.Add:
                    _processor.Emit(OpCodes.Add);
                    break;
                case AST.Types.BinaryOperation.Subtract:
                    _processor.Emit(OpCodes.Sub);
                    break;
                case AST.Types.BinaryOperation.Multiply:
                    _processor.Emit(OpCodes.Mul);
                    break;
                case AST.Types.BinaryOperation.Divide:
                    _processor.Emit(OpCodes.Div);
                    break;
                case AST.Types.BinaryOperation.Mod:
                    _processor.Emit(OpCodes.Rem);
                    break;
            }
        }

        internal void Return()
        {
            _processor.Emit(OpCodes.Ret);
        }
    }
}
