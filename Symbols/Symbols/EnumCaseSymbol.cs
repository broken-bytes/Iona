using AST.Nodes;

namespace Symbols.Symbols;

public class EnumCaseSymbol : ISymbol
{
    public string Name { get; set; }
    public string CsharpName { get; set; }
    
    public SymbolKind Kind { get; set; } = SymbolKind.EnumCase;
    public ISymbol? Parent { get; set; }
    public List<ISymbol> Symbols { get; set; } = [];

    public EnumCaseSymbol(string name, string csharpName)
    {
        Name = name;
        CsharpName = csharpName;
    }
}