using System.Text;

namespace IR;

/// <summary>
/// Walks an IrModule and produces a textual IR representation. The output
/// is a hybrid of LLVM IR and Swift SIL:
///
/// - LLVM-style: SSA register names (%0, %1), instruction mnemonics (add, sub,
///   load, store, br, cond_br, ret), basic block labels, define keyword.
/// - SIL-style: full type signatures on functions, high-level type names
///   preserved (%ModuleName.TypeName), field/property access by name,
///   mutability on fields (var/let).
///
/// Example output:
/// <code>
/// // Module: TestModule
///
/// type %TestModule.Foo = class {
///   field var "a" : i32
///   field var "b" : i32
/// }
///
/// define i32 @TestModule.Foo.compute(%TestModule.Foo %self) {
/// entry:
///   %0 = field_load %self, "a" : i32
///   %1 = field_load %self, "b" : i32
///   %2 = add i32 %0, %1
///   ret i32 %2
/// }
/// </code>
/// </summary>
public class IrPrinter
{
    /// <summary>
    /// Prints the entire module to a string.
    /// </summary>
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

    /// <summary>
    /// Prints a single function to a string.
    /// </summary>
    public string PrintFunction(IrFunction func)
    {
        var sb = new StringBuilder();
        PrintFunction(sb, func);
        return sb.ToString();
    }

    /// <summary>
    /// Prints a single type declaration to a string.
    /// </summary>
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
