using AST.Types;

namespace Symbols.Symbols
{
    public class OperatorSymbol : ISymbol
    {
        public string Name { get; set; }
        public OperatorType Operator { get; set; }
        public TypeSymbol ReturnType { get; set; }
        public List<ISymbol> Symbols { get; set; }
        public SymbolKind Kind { get; set; } = SymbolKind.Operator;
        public ISymbol? Parent { get; set; }

        public OperatorSymbol(OperatorType operatorType)
        {
            Name = operatorType.ToString();
            ReturnType = new TypeSymbol("", TypeKind.Unknown);
            Symbols = new List<ISymbol>();
        }
    }
}
