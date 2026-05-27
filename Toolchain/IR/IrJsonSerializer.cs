//|--- IrJsonSerializer.cs -------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using System.Text;
using System.Text.Json;
using IR.Instructions;

namespace IR;

public class IrJsonSerializer
{
    public string Serialize(IrModule module, bool indented = true)
    {
        using var stream = new MemoryStream();
        var options = new JsonWriterOptions { Indented = indented };
        using (var writer = new Utf8JsonWriter(stream, options))
        {
            WriteModule(writer, module);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private void WriteModule(Utf8JsonWriter w, IrModule module)
    {
        w.WriteStartObject();
        w.WriteString("schema", "iona-ir/v1");
        w.WriteString("name", module.Name);

        w.WriteStartArray("types");
        foreach (var t in module.TypeDeclarations)
            WriteTypeDecl(w, t);
        w.WriteEndArray();

        w.WriteStartArray("functions");
        foreach (var f in module.Functions)
            WriteFunction(w, f);
        w.WriteEndArray();

        w.WriteEndObject();
    }

    private void WriteTypeDecl(Utf8JsonWriter w, IrTypeDecl decl)
    {
        w.WriteStartObject();
        w.WriteString("name", decl.Name);
        w.WriteString("declKind", decl.DeclKind.ToString());

        if (decl.BaseType != null)
        {
            WriteType(w, "baseType", decl.BaseType);
        }
        else
            w.WriteNull("baseType");

        w.WriteStartArray("contracts");
        foreach (var c in decl.Contracts)
            WriteTypeValue(w, c);
        w.WriteEndArray();

        w.WriteStartArray("fields");
        foreach (var field in decl.Fields)
        {
            w.WriteStartObject();
            w.WriteString("name", field.Name);
            WriteType(w, "type", field.Type);
            w.WriteBoolean("isMutable", field.IsMutable);
            w.WriteEndObject();
        }
        w.WriteEndArray();

        w.WriteEndObject();
    }

    private void WriteFunction(Utf8JsonWriter w, IrFunction func)
    {
        w.WriteStartObject();
        w.WriteString("name", func.Name);
        WriteType(w, "returnType", func.ReturnType);
        w.WriteBoolean("isInstance", func.IsInstance);
        w.WriteBoolean("isConstructor", func.IsConstructor);

        if (func.OwningType != null)
        {
            WriteType(w, "owningType", func.OwningType);
        }
        else
            w.WriteNull("owningType");

        w.WriteStartArray("parameters");
        foreach (var p in func.Parameters)
        {
            w.WriteStartObject();
            w.WriteString("name", p.Name);
            WriteType(w, "type", p.Type);
            WriteValue(w, "value", p.Value);
            w.WriteEndObject();
        }
        w.WriteEndArray();

        w.WriteStartArray("blocks");
        foreach (var block in func.Blocks)
            WriteBlock(w, block);
        w.WriteEndArray();

        w.WriteEndObject();
    }

    private void WriteBlock(Utf8JsonWriter w, IrBasicBlock block)
    {
        w.WriteStartObject();
        w.WriteString("label", block.Label);
        w.WriteStartArray("instructions");
        foreach (var inst in block.Instructions)
            WriteInstruction(w, inst);
        w.WriteEndArray();
        w.WriteEndObject();
    }

    private void WriteInstruction(Utf8JsonWriter w, IrInstruction inst)
    {
        w.WriteStartObject();

        switch (inst)
        {
            case BinaryInst b:
                w.WriteString("op", "binary");
                w.WriteString("binaryOp", b.Op.ToString());
                WriteValue(w, "left", b.Left);
                WriteValue(w, "right", b.Right);
                WriteResult(w, b);
                break;

            case CompareInst c:
                w.WriteString("op", "compare");
                w.WriteString("compareOp", c.Op.ToString());
                WriteValue(w, "left", c.Left);
                WriteValue(w, "right", c.Right);
                WriteResult(w, c);
                break;

            case UnaryInst u:
                w.WriteString("op", "unary");
                w.WriteString("unaryOp", u.Op.ToString());
                WriteValue(w, "operand", u.Operand);
                WriteResult(w, u);
                break;

            case ConstantInst k:
                w.WriteString("op", "constant");
                w.WriteString("constantKind", k.ConstantKind.ToString());
                w.WriteString("rawValue", k.RawValue);
                WriteType(w, "constantType", k.ConstantType);
                WriteResult(w, k);
                break;

            case AllocInst a:
                w.WriteString("op", "alloc");
                w.WriteString("variableName", a.VariableName);
                WriteType(w, "allocType", a.AllocType);
                WriteResult(w, a);
                break;

            case LoadInst l:
                w.WriteString("op", "load");
                WriteValue(w, "source", l.Source);
                WriteResult(w, l);
                break;

            case StoreInst s:
                w.WriteString("op", "store");
                WriteValue(w, "value", s.Value);
                WriteValue(w, "destination", s.Destination);
                break;

            case ConvertInst cv:
                w.WriteString("op", "convert");
                WriteValue(w, "source", cv.Source);
                WriteType(w, "targetType", cv.TargetType);
                WriteResult(w, cv);
                break;

            case CallInst call:
                w.WriteString("op", "call");
                w.WriteString("targetName", call.TargetName);
                WriteType(w, "returnType", call.ReturnType);
                w.WriteBoolean("isConstructor", call.IsConstructor);
                if (call.Receiver != null)
                {
                    WriteValue(w, "receiver", call.Receiver);
                }
                else
                    w.WriteNull("receiver");
                WriteCallArgs(w, call.Arguments);
                WriteResult(w, call);
                break;

            case NewObjectInst n:
                w.WriteString("op", "new_object");
                WriteType(w, "objectType", n.ObjectType);
                if (n.ConstructorName != null)
                {
                    w.WriteString("constructorName", n.ConstructorName);
                }
                else
                    w.WriteNull("constructorName");
                WriteCallArgs(w, n.ConstructorArgs);
                WriteResult(w, n);
                break;

            case FieldLoadInst fl:
                w.WriteString("op", "field_load");
                WriteValue(w, "object", fl.Object);
                w.WriteString("fieldName", fl.FieldName);
                WriteType(w, "fieldType", fl.FieldType);
                WriteResult(w, fl);
                break;

            case FieldStoreInst fs:
                w.WriteString("op", "field_store");
                WriteValue(w, "value", fs.Value);
                WriteValue(w, "object", fs.Object);
                w.WriteString("fieldName", fs.FieldName);
                break;

            case PropertyAccessInst pa:
                w.WriteString("op", "property_access");
                WriteValue(w, "receiver", pa.Receiver);
                w.WriteString("propertyName", pa.PropertyName);
                WriteType(w, "propertyType", pa.PropertyType);
                w.WriteBoolean("isLoad", pa.IsLoad);
                if (pa.StoreValue != null)
                {
                    WriteValue(w, "storeValue", pa.StoreValue);
                }
                else
                    w.WriteNull("storeValue");
                WriteResult(w, pa);
                break;

            case ReturnInst r:
                w.WriteString("op", "return");
                if (r.ReturnValue != null)
                {
                    WriteValue(w, "returnValue", r.ReturnValue);
                }
                else
                    w.WriteNull("returnValue");
                break;

            case BranchInst br:
                w.WriteString("op", "branch");
                w.WriteString("target", br.Target.Label);
                break;

            case CondBranchInst cb:
                w.WriteString("op", "cond_br");
                WriteValue(w, "condition", cb.Condition);
                w.WriteString("thenBlock", cb.ThenBlock.Label);
                w.WriteString("elseBlock", cb.ElseBlock.Label);
                break;

            case PhiInst phi:
                w.WriteString("op", "phi");
                w.WriteStartArray("incomings");
                foreach (var inc in phi.Incomings)
                {
                    w.WriteStartObject();
                    WriteValue(w, "value", inc.Value);
                    w.WriteString("block", inc.Block.Label);
                    w.WriteEndObject();
                }
                w.WriteEndArray();
                WriteResult(w, phi);
                break;

            default:
                w.WriteString("op", "unknown");
                w.WriteString("clrType", inst.GetType().Name);
                WriteResult(w, inst);
                break;
        }

        w.WriteEndObject();
    }

    private void WriteCallArgs(Utf8JsonWriter w, List<IrCallArg> args)
    {
        w.WriteStartArray("arguments");
        foreach (var arg in args)
        {
            w.WriteStartObject();
            w.WriteString("name", arg.Name);
            WriteValue(w, "value", arg.Value);
            w.WriteEndObject();
        }
        w.WriteEndArray();
    }

    private void WriteResult(Utf8JsonWriter w, IrInstruction inst)
    {
        if (inst.Result != null)
        {
            WriteValue(w, "result", inst.Result);
        }
        else
            w.WriteNull("result");
    }

    private void WriteValue(Utf8JsonWriter w, string propertyName, IrValue value)
    {
        w.WritePropertyName(propertyName);
        w.WriteStartObject();
        w.WriteNumber("index", value.Index);
        if (value.DebugName != null)
        {
            w.WriteString("name", value.DebugName);
        }
        else
            w.WriteNull("name");
        WriteType(w, "type", value.Type);
        w.WriteEndObject();
    }

    private void WriteType(Utf8JsonWriter w, string propertyName, IrType type)
    {
        w.WritePropertyName(propertyName);
        WriteTypeValue(w, type);
    }

    private void WriteTypeValue(Utf8JsonWriter w, IrType type)
    {
        w.WriteStartObject();
        switch (type)
        {
            case IrPrimitiveType p:
                w.WriteString("kind", "primitive");
                w.WriteString("primitive", p.Primitive.ToString());
                break;
            case IrClassType c:
                w.WriteString("kind", "class");
                w.WriteString("name", c.FullyQualifiedName);
                break;
            case IrStructType s:
                w.WriteString("kind", "struct");
                w.WriteString("name", s.FullyQualifiedName);
                break;
            case IrEnumType e:
                w.WriteString("kind", "enum");
                w.WriteString("name", e.FullyQualifiedName);
                break;
            case IrArrayType a:
                w.WriteString("kind", "array");
                WriteType(w, "element", a.ElementType);
                break;
            default:
                w.WriteString("kind", "unknown");
                w.WriteString("name", type.Name);
                break;
        }
        w.WriteEndObject();
    }
}
