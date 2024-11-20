namespace Shared;

public static class Utils
{
    public static string IonaToCSharpName(string ionaName)
    {
        // The first character needs to be uppercase.
        var cSharpName = ionaName[0].ToString().ToUpper() + ionaName.Substring(1);

        return cSharpName;
    }

    public static string CSharpToIonaName(string cSharpName)
    {
        // The first char needs to be lowercase
        var ionaName = cSharpName[0].ToString().ToLower() + cSharpName.Substring(1);
        
        return ionaName;
    }
    
    /// <summary>
    /// Used to get the boxed type. This automatically converts Iona's Builtin Types like `Int` to the C# primitive `int` etc.
    /// </summary>
    /// <param name="name">The Full name of the type</param>
    /// <returns></returns>
    public static string GetBoxedName(string fullName)
    {
        switch (fullName)
        {
            case "Iona.Builtins.Bool":
                return "bool";
            case "Iona.Builtins.Double":
                return "double";
            case "Iona.Builtins.Float":
                return "float";
            case "Iona.Builtins.Int8":
                return "sbyte";
            case "Iona.Builtins.Int16":
                return "short";
            case "Iona.Builtins.Int32":
                return "int";
            case "Iona.Builtins.Int64":
                return "long";
            case "Iona.Builtins.Int":
                return "nint";
            case "Iona.Builtins.UInt8":
                return "byte";
            case "Iona.Builtins.UInt16":
                return "ushort";
            case "Iona.Builtins.UInt32":
                return "uint";
            case "Iona.Builtins.UInt64":
                return "ulong";
            case "Iona.Builtins.UInt":
                return "nuint";
            case "Iona.Builtins.String":
                return "string";
            case "Iona.Builtins.Void":
                return "void";
        }

        return fullName;
    }
    
    // <summary>
    /// Used to get the unxboed type. This automatically converts C#s primitive types like `int` to Iona's Builtin Types like `Int` etc.
    /// </summary>
    /// <param name="name">The Full name of the type</param>
    /// <returns></returns>
    public static string GetUnboxedName(string fullName)
    {
        switch (fullName)
        {
            case "bool":
                return "Iona.Builtins.Bool";
            case "double":
                return "Iona.Builtins.Double";
            case "float":
            case "System.Single":
                return "Iona.Builtins.Float";
            case "sbyte":
                return "Iona.Builtins.Int8";
            case "short":
                return "Iona.Builtins.Int16";
            case "int":
            case "System.Int32":
                return "Iona.Builtins.Int32";
            case "long":
                return "Iona.Builtins.Int64";
            case "nint":
                return "Iona.Builtins.NInt";
            case "byte":
                return "Iona.Builtins.UInt8";
            case "ushort":
                return "Iona.Builtins.UInt16";
            case "uint":
                return "Iona.Builtins.UInt32";
            case "ulong":
                return "Iona.Builtins.UInt64";
            case "nuint":
                return "Iona.Builtins.NUInt";
            case "string":
                return "Iona.Builtins.String";
            case "void":
                return "Iona.Builtins.Void";
        }

        return fullName;
    }
}