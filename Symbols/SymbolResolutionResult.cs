namespace Symbols;


public struct SymbolResolutionResult
{
    public SymbolResolutionError Error;
    /// Used to indicate where the ambiguity is when the error is `Ambiguous`. Holds the ambiguous modules
    public List<string> Ambiguity;
}

public enum SymbolResolutionError
{
    NotFound,
    Ambigious
}