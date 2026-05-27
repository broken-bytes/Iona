//|--- IrPrinter.cs --------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using System.Text;

namespace IR;

public class IrPrinter
{
    public string Print(IrModule module)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"// Module: {module.Name}");
        sb.AppendLine();

        // Print type declarations
        foreach (var typeDecl in module.TypeDeclarations)
        {
            PrintTypeDecl(sb, typeDecl);
            sb.AppendLine();
        }

        // Print functions
        for (int i = 0; i < module.Functions.Count; i++)
        {
            PrintFunction(sb, module.Functions[i]);
            if (i < module.Functions.Count - 1)
                sb.AppendLine();
        }

        return sb.ToString().TrimEnd() + "\n";
    }

    public string PrintFunction(IrFunction func)
    {
        var sb = new StringBuilder();
        PrintFunction(sb, func);
        return sb.ToString();
    }

    public string PrintTypeDecl(IrTypeDecl typeDecl)
    {
        var sb = new StringBuilder();
        PrintTypeDecl(sb, typeDecl);
        return sb.ToString();
    }

    // ------------------------------------------------------------------
    // Type declarations
    // ------------------------------------------------------------------

    private void PrintTypeDecl(StringBuilder sb, IrTypeDecl typeDecl)
    {
        var kindStr = typeDecl.DeclKind switch
        {
            IrTypeDeclKind.Class => "class",
            IrTypeDeclKind.Struct => "struct",
            IrTypeDeclKind.Enum => "enum",
            _ => "type"
        };

        // Build extends/conforms suffix
        var suffix = "";
        if (typeDecl.BaseType != null)
        {
            suffix += $" extends {typeDecl.BaseType}";
        }

        if (typeDecl.Contracts.Count > 0)
        {
            var contracts = string.Join(", ", typeDecl.Contracts.Select(c => c.ToString()));
            suffix += $" conforms_to {contracts}";
        }

        if (typeDecl.Fields.Count == 0)
        {
            sb.AppendLine($"type %{typeDecl.Name} = {kindStr}{suffix} {{}}");
            return;
        }

        sb.AppendLine($"type %{typeDecl.Name} = {kindStr}{suffix} {{");

        foreach (var field in typeDecl.Fields)
        {
            var mutStr = field.IsMutable ? "var" : "let";
            sb.AppendLine($"  field {mutStr} \"{field.Name}\" : {field.Type}");
        }

        sb.AppendLine("}");
    }

    // ------------------------------------------------------------------
    // Functions
    // ------------------------------------------------------------------

    private void PrintFunction(StringBuilder sb, IrFunction func)
    {
        // Build parameter list in LLVM-style: (Type %name, Type %name, ...)
        var paramParts = new List<string>();

        // Instance methods have an implicit @self parameter
        if (func.IsInstance && func.OwningType != null)
        {
            paramParts.Add($"{func.OwningType} %self");
        }

        foreach (var param in func.Parameters)
        {
            paramParts.Add($"{param.Type} {param.Value}");
        }

        var paramStr = string.Join(", ", paramParts);
        var keyword = func.IsConstructor ? "define_ctor" : "define";

        sb.AppendLine($"{keyword} {func.ReturnType} @{func.Name}({paramStr}) {{");

        foreach (var block in func.Blocks)
        {
            PrintBlock(sb, block);
        }

        sb.AppendLine("}");
    }

    // ------------------------------------------------------------------
    // Basic blocks
    // ------------------------------------------------------------------

    private void PrintBlock(StringBuilder sb, IrBasicBlock block)
    {
        sb.AppendLine($"{block.Label}:");

        foreach (var inst in block.Instructions)
        {
            sb.AppendLine($"  {inst.Print()}");
        }
    }
}
