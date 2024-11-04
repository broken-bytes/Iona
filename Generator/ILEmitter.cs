using AST.Nodes;
using AST.Types;
using Generator.Types;
using Symbols;
using Symbols.Symbols;
using System.Text;

namespace Generator
{
    internal class ILEmitter
    {
        private StringBuilder _stream;
        private ILProcessor _processor;

        internal ILEmitter() {
            _stream = new StringBuilder();
        }

        internal void SetILProcessor(ILProcessor processor) 
        {
            _processor = processor;
        }

        // ---- Boxing ----
        internal void Box(TypeReferenceNode from, TypeReferenceNode to)
        {
            _processor.Emit(OpCode.Box, from);
            _processor.Emit(OpCode.Castclass, to);
        }

        internal void Unbox(TypeReferenceNode to)
        {
            _processor.Emit(OpCode.Unbox_Any, to);
        }

        // ---- Get emitters ----
        internal void Call(FuncCallNode methodRef)
        {
            _processor.Emit(OpCode.Call, methodRef);
        }

        internal void CreateObject(TypeSymbol type, InitSymbol ctor)
        {
            // Check the args to be used
            _processor.Emit(OpCode.Newobj);
            _processor.Emit(OpCode.Void);
            _processor.Emit(OpCode.Call, ctor);
        }

        internal void GetArg(int index)
        {
            _processor.Emit(OpCode.LoadArgument, index);
        }

        internal void GetField(FieldDefinition fieldDef)
        {
            _processor.Emit(OpCode.LoadField, fieldDef.Index);
        }

        internal void GetLiteral(LiteralNode literal)
        {
            switch (literal.LiteralType)
            {
                case LiteralType.String:
                    _processor.Emit(OpCode.LoadString, literal.Value);
                    break;
                case LiteralType.Integer:
                    _processor.Emit(OpCode.LoadInt, int.Parse(literal.Value));
                    break;
                case LiteralType.Float:
                    _processor.Emit(OpCode.LoadFloat, float.Parse(literal.Value));
                    break;
                case LiteralType.Double:
                    _processor.Emit(OpCode.LoadDouble, double.Parse(literal.Value));
                    break;
                case LiteralType.Boolean:
                    _processor.Emit(bool.Parse(literal.Value) ? OpCode.LoadTrue : OpCode.LoadFalse);
                    break;
            }
        }

        internal void GetProperty(PropAccessNode propDef)
        {
            //var symbol = _table.FindBy(propDef.Object);
            //_processor.Emit(OpCode.Call, propDef.GetMethod);
        }

        internal void GetThis()
        {
            _processor.Emit(OpCode.LoadArgument, 0);
        }

        internal void GetVariable(int index)
        {
            _processor.Emit(OpCode.LoadVariable, index);
        }

        // ---- Set emitters ----

        internal void SetField(FieldDefinition fieldDef)
        {
            _processor.Emit(OpCode.SetField, fieldDef.Index);
        }

        internal void SetProperty(PropAccessNode propDef)
        {
            //_processor.Emit(OpCode.Call, propDef.SetMethod);
        }

        internal void SetVariable(int index)
        {
            _processor.Emit(OpCode.SetVariable, index);
        }

        // ---- Operation emitters ----

        internal void BinaryOperation(BinaryOperation operation)
        {
            switch (operation)
            {
                case AST.Types.BinaryOperation.Add:
                    _processor.Emit(OpCode.Add);
                    break;
                case AST.Types.BinaryOperation.Subtract:
                    _processor.Emit(OpCode.Subtract);
                    break;
                case AST.Types.BinaryOperation.Multiply:
                    _processor.Emit(OpCode.Multiply);
                    break;
                case AST.Types.BinaryOperation.Divide:
                    _processor.Emit(OpCode.Divide);
                    break;
                case AST.Types.BinaryOperation.Mod:
                    _processor.Emit(OpCode.Mod);
                    break;
            }
        }

        internal void Return()
        {
            _processor.Emit(OpCode.Return);
        }
    }
}
