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
}